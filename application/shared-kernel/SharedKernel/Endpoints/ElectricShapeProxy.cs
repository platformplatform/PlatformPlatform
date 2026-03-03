using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using SharedKernel.ExecutionContext;

namespace SharedKernel.Endpoints;

public static class ElectricShapeProxy
{
    private static readonly HttpClient ElectricHttpClient = new() { Timeout = Timeout.InfiniteTimeSpan };

    public static async Task ProxyShapeRequest(HttpContext httpContext, IExecutionContext executionContext, IConfiguration configuration, Dictionary<string, string> allowedTables)
    {
        var tenantId = executionContext.TenantId;
        if (tenantId is null)
        {
            httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        var query = httpContext.Request.Query;
        var table = query["table"].ToString();

        if (string.IsNullOrEmpty(table) || !allowedTables.TryGetValue(table, out var tenantColumn))
        {
            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        var electricUrl = configuration["ELECTRIC_URL"];
        if (string.IsNullOrEmpty(electricUrl))
        {
            httpContext.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            return;
        }

        var upstreamParameters = new Dictionary<string, string>();

        foreach (var param in query)
        {
            if (param.Key is not "where")
            {
                upstreamParameters[param.Key] = param.Value.ToString();
            }
        }

        upstreamParameters["where"] = $"{tenantColumn}='{tenantId.Value}'";

        var queryString = string.Join("&", upstreamParameters.Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));
        var requestUri = $"{electricUrl.TrimEnd('/')}/v1/shape?{queryString}";

        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);

        var ifNoneMatch = httpContext.Request.Headers.IfNoneMatch.ToString();
        if (!string.IsNullOrEmpty(ifNoneMatch))
        {
            request.Headers.TryAddWithoutValidation("If-None-Match", ifNoneMatch);
        }

        using var response = await ElectricHttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, httpContext.RequestAborted);

        httpContext.Response.StatusCode = (int)response.StatusCode;

        foreach (var header in response.Headers)
        {
            httpContext.Response.Headers[header.Key] = header.Value.ToArray();
        }

        foreach (var header in response.Content.Headers)
        {
            httpContext.Response.Headers[header.Key] = header.Value.ToArray();
        }

        httpContext.Response.Headers.Remove("transfer-encoding");

        await response.Content.CopyToAsync(httpContext.Response.Body, httpContext.RequestAborted);
    }
}
