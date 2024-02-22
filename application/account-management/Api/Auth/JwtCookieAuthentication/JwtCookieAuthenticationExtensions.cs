using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PlatformPlatform.AccountManagement.Infrastructure;
using IdentityUser = PlatformPlatform.AccountManagement.Infrastructure.Identity.IdentityUser;

namespace PlatformPlatform.AccountManagement.Api.Auth.JwtCookieAuthentication;

public static class JwtCookieAuthenticationExtensions
{
    [UsedImplicitly]
    public static IServiceCollection AddAuthenticationServices(this IServiceCollection services)
    {
        var jwtSecretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") ??
                           throw new InvalidOperationException("JWT_SECRET_KEY environment variable is not set.");
        var jwtSecurityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey));

        services.Configure<IdentityOptions>(options => options.User.RequireUniqueEmail = false);

        var authenticationBuilder = services
            .AddAuthentication(options =>
            {
                options.DefaultScheme = JwtCookieAuthenticationOptions.DefaultScheme;
                options.DefaultAuthenticateScheme = JwtCookieAuthenticationOptions.DefaultScheme;
                options.DefaultSignInScheme = JwtCookieAuthenticationOptions.DefaultScheme;
                options.DefaultSignOutScheme = JwtCookieAuthenticationOptions.DefaultScheme;
                options.DefaultChallengeScheme = JwtCookieAuthenticationOptions.DefaultScheme;
                options.DefaultForbidScheme = JwtCookieAuthenticationOptions.DefaultScheme;
            });

        authenticationBuilder.Services.TryAddEnumerable(ServiceDescriptor
            .Singleton<IConfigureOptions<JwtCookieAuthenticationOptions>, JwtCookieConfigureOptions>());

        authenticationBuilder.AddScheme<JwtCookieAuthenticationOptions, JwtCookieAuthenticationHandler>(
            JwtCookieAuthenticationOptions.DefaultScheme,
            (Action<JwtCookieAuthenticationOptions>)(options =>
            {
                options.SigningSecurityKey = jwtSecurityKey;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = "localhost",
                    ValidAudience = "localhost",
                    IssuerSigningKeys = new[] { jwtSecurityKey }
                };
            }));

        services
            .AddAuthorizationBuilder()
            .AddPolicy(RequireOwnerRole.Name, policy => policy.RequireClaim(ClaimTypes.Role, RequireOwnerRole.Value))
            .AddPolicy(RequireUserRole.Name, policy => policy.RequireClaim(ClaimTypes.Role, RequireUserRole.Value));

        services
            .AddIdentityApiEndpoints<IdentityUser>(options => { options.SignIn.RequireConfirmedAccount = true; })
            .AddEntityFrameworkStores<AccountManagementDbContext>();

        services.AddSingleton<IEmailSender<IdentityUser>, IdentityEmailTestSender>();

        return services;
    }
}

public static class RequireOwnerRole
{
    public const string Name = nameof(RequireOwnerRole);
    public const string Value = "Owner";
}

public static class RequireUserRole
{
    public const string Name = nameof(RequireUserRole);
    public const string Value = "Owner";
}