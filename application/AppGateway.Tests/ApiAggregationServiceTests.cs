using System.Net;
using System.Text;
using AppGateway.ApiAggregation;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;
using SharedKernel.Configuration;
using Xunit;
using Yarp.ReverseProxy.Configuration;

namespace AppGateway.Tests;

public sealed class ApiAggregationServiceTests
{
    private const string AccountOpenApiJson = """
                                              {
                                                "openapi": "3.0.0",
                                                "info": { "title": "PlatformPlatform Account API", "version": "v1" },
                                                "paths": {
                                                  "/api/account/users": { "get": { "responses": { "200": { "description": "OK" } } } },
                                                  "/internal-api/account/probe": { "get": { "responses": { "200": { "description": "OK" } } } }
                                                }
                                              }
                                              """;

    private const string MainOpenApiJson = """
                                           {
                                             "openapi": "3.0.0",
                                             "info": { "title": "PlatformPlatform Main API", "version": "v1" },
                                             "paths": {
                                               "/api/main/health": { "get": { "responses": { "200": { "description": "OK" } } } }
                                             }
                                           }
                                           """;

    // Simulates a future endpoint that accidentally leaks into the account document with a
    // /api/back-office prefix; the aggregator's belt-and-braces filter must drop it.
    private const string AccountOpenApiJsonWithLeakedBackOfficePath = """
                                                                      {
                                                                        "openapi": "3.0.0",
                                                                        "info": { "title": "PlatformPlatform Account API", "version": "v1" },
                                                                        "paths": {
                                                                          "/api/account/users": { "get": { "responses": { "200": { "description": "OK" } } } },
                                                                          "/api/back-office/leaked": { "get": { "responses": { "200": { "description": "OK" } } } }
                                                                        }
                                                                      }
                                                                      """;

    [Fact]
    public async Task GetAggregatedOpenApiJson_ShouldIncludeAccountApiPaths()
    {
        // Arrange
        var service = CreateService(AccountOpenApiJson, MainOpenApiJson);

        // Act
        var aggregated = await service.GetAggregatedOpenApiJson();

        // Assert
        aggregated.Should().Contain("\"/api/account/users\"");
    }

    [Fact]
    public async Task GetAggregatedOpenApiJson_ShouldIncludeMainApiPaths()
    {
        // Arrange
        var service = CreateService(AccountOpenApiJson, MainOpenApiJson);

        // Act
        var aggregated = await service.GetAggregatedOpenApiJson();

        // Assert
        aggregated.Should().Contain("\"/api/main/health\"");
    }

    [Fact]
    public async Task GetAggregatedOpenApiJson_ShouldExcludeBackOfficePaths()
    {
        // Arrange
        var service = CreateService(AccountOpenApiJsonWithLeakedBackOfficePath, MainOpenApiJson);

        // Act
        var aggregated = await service.GetAggregatedOpenApiJson();

        // Assert
        aggregated.Should().NotContain("/api/back-office/");
        aggregated.Should().Contain("\"/api/account/users\"");
    }

    [Fact]
    public async Task GetAggregatedOpenApiJson_ShouldExcludeInternalApiPaths()
    {
        // Arrange
        var service = CreateService(AccountOpenApiJson, MainOpenApiJson);

        // Act
        var aggregated = await service.GetAggregatedOpenApiJson();

        // Assert
        aggregated.Should().NotContain("/internal-api/");
    }

    private static ApiAggregationService CreateService(string accountDocumentJson, string mainDocumentJson)
    {
        var handler = new StubHttpMessageHandler(request =>
            {
                var requestUrl = request.RequestUri!.ToString();
                if (requestUrl.EndsWith("/openapi/account.json", StringComparison.OrdinalIgnoreCase))
                {
                    return BuildJsonResponse(accountDocumentJson);
                }

                if (requestUrl.EndsWith("/openapi/v1.json", StringComparison.OrdinalIgnoreCase))
                {
                    return BuildJsonResponse(mainDocumentJson);
                }

                return new HttpResponseMessage(HttpStatusCode.NotFound) { ReasonPhrase = $"Unstubbed URL: {requestUrl}" };
            }
        );

        var clusters = new ClusterConfig[]
        {
            new() { ClusterId = "account-api", Destinations = new Dictionary<string, DestinationConfig> { ["destination"] = new() { Address = "https://placeholder.invalid" } } },
            new() { ClusterId = "main-api", Destinations = new Dictionary<string, DestinationConfig> { ["destination"] = new() { Address = "https://placeholder.invalid" } } }
        };

        var proxyConfigProvider = new StubProxyConfigProvider(new StubProxyConfig(clusters));
        var httpClientFactory = new StubHttpClientFactory(handler);
        var ports = new PortAllocation(9000);
        return new ApiAggregationService(NullLogger<ApiAggregationService>.Instance, proxyConfigProvider, httpClientFactory, ports);
    }

    private static HttpResponseMessage BuildJsonResponse(string json)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(responder(request));
        }
    }

    private sealed class StubHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name)
        {
            return new HttpClient(handler, false);
        }
    }

    private sealed class StubProxyConfigProvider(IProxyConfig config) : IProxyConfigProvider
    {
        public IProxyConfig GetConfig()
        {
            return config;
        }
    }

    private sealed class StubProxyConfig(IReadOnlyList<ClusterConfig> clusters) : IProxyConfig
    {
        public IReadOnlyList<RouteConfig> Routes => Array.Empty<RouteConfig>();

        public IReadOnlyList<ClusterConfig> Clusters => clusters;

        public IChangeToken ChangeToken => new CancellationChangeToken(CancellationToken.None);
    }
}
