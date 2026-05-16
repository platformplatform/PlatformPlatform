using System.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NJsonSchema.Generation;
using SharedKernel.Antiforgery;
using SharedKernel.Authentication;
using SharedKernel.Authentication.BackOfficeIdentity;
using SharedKernel.Authentication.MockEasyAuth;
using SharedKernel.Endpoints;
using SharedKernel.ExecutionContext;
using SharedKernel.Middleware;
using SharedKernel.OpenApi;
using SharedKernel.Platform;
using SharedKernel.SinglePageApp;
using SharedKernel.StronglyTypedIds;
using SharedKernel.Telemetry;
using IPNetwork = System.Net.IPNetwork;

namespace SharedKernel.Configuration;

public static class ApiDependencyConfiguration
{
    private const string LocalhostCorsPolicyName = "LocalhostCorsPolicy";

    // The frontend cannot set Accept-Language from JavaScript: the Fetch spec lists it as a
    // forbidden request header, so Firefox and WebKit silently drop it (only Chromium currently
    // allows it). The SPA's API client therefore sends X-Locale instead, and this culture provider
    // is registered ahead of the default Accept-Language provider so the JS-supplied locale wins
    // when present. Accept-Language remains as a fallback for legitimate browser-driven requests
    // (e.g. the initial HTML navigation, server-rendered fallbacks).
    private const string LocaleHeaderName = "X-Locale";

    private static readonly string LocalhostUrl = Environment.GetEnvironmentVariable(SinglePageAppConfiguration.PublicUrlKey)!;

    private static RequestLocalizationOptions BuildApiRequestLocalizationOptions()
    {
        var options = new RequestLocalizationOptions()
            .SetDefaultCulture(SinglePageAppConfiguration.SupportedLocalizations[0])
            .AddSupportedCultures(SinglePageAppConfiguration.SupportedLocalizations)
            .AddSupportedUICultures(SinglePageAppConfiguration.SupportedLocalizations);

        options.RequestCultureProviders.Insert(0, new CustomRequestCultureProvider(context =>
                {
                    var headerValue = context.Request.Headers[LocaleHeaderName].ToString();
                    if (string.IsNullOrWhiteSpace(headerValue)) return Task.FromResult<ProviderCultureResult?>(null);

                    return Task.FromResult<ProviderCultureResult?>(new ProviderCultureResult(headerValue));
                }
            )
        );

        return options;
    }

    extension(WebApplicationBuilder builder)
    {
        public WebApplicationBuilder AddApiInfrastructure()
        {
            if (builder.Environment.IsDevelopment())
            {
                builder.Services.AddCors(options => options.AddPolicy(
                        LocalhostCorsPolicyName,
                        policyBuilder => { policyBuilder.WithOrigins(LocalhostUrl).AllowAnyMethod().AllowAnyHeader(); }
                    )
                );
            }

            builder.WebHost.ConfigureKestrel(options => { options.AddServerHeader = false; });
            return builder;
        }

        public WebApplicationBuilder AddDevelopmentPort()
        {
            // KESTREL_PORT is set by AppHost from the base port in .workspace/port.txt.
            // BACK_OFFICE_KESTREL_PORT is optional; AppHost sets it on the consolidated account-api so
            // back-office.dev.localhost can hit Kestrel directly without traversing AppGateway,
            // mirroring the production split where back-office is its own ACA container app with its
            // own external ingress. Outside Aspire (e.g. unit tests via WebApplicationFactory)
            // ConfigureKestrel is not invoked.
            builder.WebHost.ConfigureKestrel((context, serverOptions) =>
                {
                    if (!context.HostingEnvironment.IsDevelopment()) return;

                    if (!int.TryParse(Environment.GetEnvironmentVariable("KESTREL_PORT"), out var port) || port <= 0)
                    {
                        throw new InvalidOperationException(
                            "KESTREL_PORT environment variable is required for development startup. Run via Aspire AppHost or set KESTREL_PORT manually."
                        );
                    }

                    serverOptions.ConfigureEndpointDefaults(listenOptions => listenOptions.UseHttps());
                    serverOptions.ListenLocalhost(port, listenOptions => listenOptions.UseHttps());

                    if (int.TryParse(Environment.GetEnvironmentVariable("BACK_OFFICE_KESTREL_PORT"), out var backOfficePort) && backOfficePort > 0)
                    {
                        serverOptions.ListenLocalhost(backOfficePort, listenOptions => listenOptions.UseHttps());
                    }
                }
            );
            return builder;
        }
    }

    extension(IServiceCollection services)
    {
        public IServiceCollection AddApiServices(Assembly[] assemblies, ApiDocumentLayout documentLayout = ApiDocumentLayout.Single)
        {
            services
                .AddApiExecutionContext()
                .AddExceptionHandler<GlobalExceptionHandler>()
                .AddTransient<TelemetryContextMiddleware>()
                .AddTransient<ModelBindingExceptionHandlerMiddleware>()
                .AddTransient<AntiforgeryMiddleware>()
                .AddProblemDetails()
                .AddEndpointsApiExplorer()
                .AddApiEndpoints(assemblies)
                .AddOpenApiConfiguration(assemblies, documentLayout)
                .AddAuthConfiguration()
                .AddAntiforgery(options =>
                    {
                        options.Cookie.Name = AuthenticationTokenHttpKeys.AntiforgeryTokenCookieName;
                        options.HeaderName = AuthenticationTokenHttpKeys.AntiforgeryTokenHttpHeaderKey;
                    }
                )
                .AddHttpForwardHeaders();

            // BackOffice:Host is required only when this API hosts the back-office route group.
            // Other APIs (e.g. Main.Api, the legacy back-office/Api kept until PP-1149) skip the
            // option so their startup doesn't fail validation for a config they never use.
            if (documentLayout == ApiDocumentLayout.AccountAndBackOffice)
            {
                services.AddOptions<BackOfficeHostOptions>()
                    .BindConfiguration(BackOfficeHostOptions.SectionName)
                    .ValidateDataAnnotations()
                    .ValidateOnStart();
            }

            return services;
        }

        private IServiceCollection AddApiExecutionContext()
        {
            // Add the execution context service that will be used to make current user information available to the application.
            // OpenTelemetryEnricher depends on IExecutionContext, so it lives here -- AppGateway has no per-request execution
            // context and only emits transport-level traces via AddSharedTelemetry.
            return services
                .AddScoped<IExecutionContext, HttpExecutionContext>()
                .AddScoped<OpenTelemetryEnricher>();
        }
    }

    extension(WebApplication app)
    {
        public WebApplication UseApiServices()
        {
            if (app.Environment.IsDevelopment())
            {
                // Enable the developer exception page, which displays detailed information about exceptions that occur
                app.UseDeveloperExceptionPage();
                app.UseCors(LocalhostCorsPolicyName);
            }
            else
            {
                // Configure global exception handling for the production environment
                app.UseExceptionHandler(_ => { });
            }

            app
                .UseForwardedHeaders()
                .UseRouting() // Explicit so it runs AFTER UseForwardedHeaders. Without this, ASP.NET Core inserts UseRouting at the start of the pipeline and the SPA-host fallback (UseHostScopedSinglePageAppFallback's RequireHost) sees the unrewritten Host header forwarded by YARP (the destination address), not the public host promoted from X-Forwarded-Host.
                .UseRequestLocalization(BuildApiRequestLocalizationOptions()) // Must run before endpoint execution so handlers can read the request culture via IExecutionContext.UserInfo.Locale (e.g. anonymous email senders that have no User row to read Locale from). The SPA fallback's UseRequestLocalization at the end of the pipeline only covers HTML fallback requests, not API endpoints.
                .UseMockEasyAuthInDevelopment() // Dev-only: serve /.auth/login/aad and inject X-MS-CLIENT-PRINCIPAL-* headers from a dev cookie. Must run before authentication.
                .UseAuthentication() // Must be above TelemetryContextMiddleware to ensure authentication happens first
                .UseAuthorization()
                .UseAntiforgery()
                .UseMiddleware<AntiforgeryMiddleware>()
                .UseMiddleware<TelemetryContextMiddleware>() // It must be above ModelBindingExceptionHandlerMiddleware to ensure that model binding problems are annotated correctly
                .UseMiddleware<ModelBindingExceptionHandlerMiddleware>() // Enable support for proxy headers such as X-Forwarded-For and X-Forwarded-Proto. Should run before other middleware
                .UseOpenApi(options => options.Path = "/openapi/{documentName}.json"); // Adds the OpenAPI generator that uses the ASP. NET Core API Explorer; one route per registered document

            return app.UseApiEndpoints();
        }
    }

    extension(IServiceCollection services)
    {
        private IServiceCollection AddApiEndpoints(Assembly[] assemblies)
        {
            return services
                .Scan(scan => scan
                    .FromAssemblies(assemblies.Concat([Assembly.GetExecutingAssembly()]).ToArray())
                    .AddClasses(classes => classes.AssignableTo<IEndpoints>(), false)
                    .AsImplementedInterfaces()
                    .WithScopedLifetime()
                );
        }
    }

    extension(WebApplication app)
    {
        private WebApplication UseApiEndpoints()
        {
            // Manually create all endpoint classes to call the MapEndpoints containing the mappings
            using var scope = app.Services.CreateScope();
            var endpointServices = scope.ServiceProvider.GetServices<IEndpoints>();
            foreach (var endpoint in endpointServices)
            {
                endpoint.MapEndpoints(app);
            }

            return app;
        }
    }

    extension(IServiceCollection services)
    {
        private IServiceCollection AddOpenApiConfiguration(Assembly[] assemblies, ApiDocumentLayout documentLayout)
        {
            var allAssemblies = assemblies.Concat([Assembly.GetExecutingAssembly()]).ToArray();

            if (documentLayout == ApiDocumentLayout.Single)
            {
                services.AddOpenApiDocument((settings, _) =>
                    {
                        settings.DocumentName = "v1";
                        settings.Title = $"{Settings.Current.Branding.ProductName} API";
                        settings.Version = "v1";

                        var options = (SystemTextJsonSchemaGeneratorSettings)settings.SchemaSettings;
                        options.SerializerOptions = SharedDependencyConfiguration.DefaultJsonSerializerOptions;
                        settings.DocumentProcessors.Add(new StronglyTypedDocumentProcessor(allAssemblies));
                        settings.DocumentProcessors.Add(new PublicApiEnumDocumentProcessor(allAssemblies));
                    }
                );
                return services;
            }

            services.AddOpenApiDocument((settings, _) =>
                {
                    settings.DocumentName = OpenApiDocumentNames.Account;
                    settings.Title = $"{Settings.Current.Branding.ProductName} Account API";
                    settings.Version = "v1";
                    settings.ApiGroupNames = [OpenApiDocumentNames.Account];

                    var options = (SystemTextJsonSchemaGeneratorSettings)settings.SchemaSettings;
                    options.SerializerOptions = SharedDependencyConfiguration.DefaultJsonSerializerOptions;
                    settings.DocumentProcessors.Add(new StronglyTypedDocumentProcessor(allAssemblies));
                    settings.DocumentProcessors.Add(new PublicApiEnumDocumentProcessor(allAssemblies));
                }
            );

            services.AddOpenApiDocument((settings, _) =>
                {
                    settings.DocumentName = OpenApiDocumentNames.BackOffice;
                    settings.Title = $"{Settings.Current.Branding.ProductName} Back Office API";
                    settings.Version = "v1";
                    settings.ApiGroupNames = [OpenApiDocumentNames.BackOffice];

                    var options = (SystemTextJsonSchemaGeneratorSettings)settings.SchemaSettings;
                    options.SerializerOptions = SharedDependencyConfiguration.DefaultJsonSerializerOptions;
                    settings.DocumentProcessors.Add(new StronglyTypedDocumentProcessor(allAssemblies));
                    settings.DocumentProcessors.Add(new PublicApiEnumDocumentProcessor(allAssemblies));
                }
            );

            return services;
        }

        private IServiceCollection AddAuthConfiguration()
        {
            // Add Authentication and Authorization services
            services
                .AddAuthentication(options =>
                    {
                        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                    }
                )
                .AddJwtBearer(o =>
                    {
                        var tokenSigningService = SharedDependencyConfiguration.GetTokenSigningService();
                        o.TokenValidationParameters = tokenSigningService.GetTokenValidationParameters(
                            validateLifetime: true,
                            clockSkew: TimeSpan.FromSeconds(5) // In Azure, we don't need any clock skew, but this must be a higher value than the AppGateway
                        );
                    }
                )
                .AddScheme<AuthenticationSchemeOptions, BackOfficeIdentityHandler>(BackOfficeIdentityDefaults.AuthenticationScheme, _ => { });

            services.AddSingleton<IAuthorizationHandler, BackOfficeAdminAuthorizationHandler>();

            return services.AddAuthorization(authOptions =>
                {
                    authOptions.AddPolicy(BackOfficeIdentityDefaults.PolicyName, policy =>
                        {
                            policy.AuthenticationSchemes = [BackOfficeIdentityDefaults.AuthenticationScheme];
                            policy.RequireAuthenticatedUser();
                        }
                    );
                    authOptions.AddPolicy(BackOfficeIdentityDefaults.AdminPolicyName, policy =>
                        {
                            policy.AuthenticationSchemes = [BackOfficeIdentityDefaults.AuthenticationScheme];
                            policy.RequireAuthenticatedUser();
                            policy.AddRequirements(new BackOfficeAdminRequirement());
                        }
                    );
                }
            );
        }

        public IServiceCollection AddHttpForwardHeaders()
        {
            // Ensure correct client IP addresses are set for requests
            // This is required when running behind a reverse proxy like YARP or Azure Container Apps
            return services.Configure<ForwardedHeadersOptions>(options =>
                {
                    // X-Forwarded-For surfaces real upstream client IPs in Application Insights / logs.
                    // X-Forwarded-Proto sets Request.Scheme so generated absolute URLs use https.
                    // X-Forwarded-Host is intentionally NOT enabled: each Azure container app
                    // registers only the SPA it serves (see Account.Api Program.cs), so endpoint
                    // matching no longer depends on Request.Host being rewritten from forwarded
                    // headers, and dev's dual-Kestrel setup delivers the right Host directly.
                    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                    options.ForwardLimit = 1;
                    // Honor forwarded headers only from trusted proxies. Loopback covers the Aspire
                    // localhost stack. RFC 6598 (100.64.0.0/10) covers ACA's mesh envoy, which
                    // presents from shared address space rather than from the customer VNet -- it is
                    // the immediate peer for both AppGateway pods and internal account-api / main-api
                    // pods. Externally-exposed services sit behind that envoy, which strips
                    // client-supplied X-Forwarded-* and sets its own, so trusting this range does not
                    // let external callers spoof the headers. PP-1066: keeps trust scoped to networks
                    // we control.
                    options.KnownIPNetworks.Clear();
                    options.KnownIPNetworks.Add(new IPNetwork(IPAddress.Parse("127.0.0.0"), 8));
                    options.KnownIPNetworks.Add(new IPNetwork(IPAddress.IPv6Loopback, 128));
                    options.KnownIPNetworks.Add(new IPNetwork(IPAddress.Parse("100.64.0.0"), 10));
                    options.KnownProxies.Clear();
                }
            );
        }
    }
}
