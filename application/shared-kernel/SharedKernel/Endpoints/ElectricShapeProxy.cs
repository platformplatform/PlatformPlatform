using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using SharedKernel.ExecutionContext;

namespace SharedKernel.Endpoints;

public sealed record ShapeTableConfig(string TenantColumn, string[] Columns, string? RequiredRole = null, string? UserScopedColumn = null);

public static class ElectricShapeProxy
{
    private static readonly HttpClient ElectricHttpClient = new() { Timeout = Timeout.InfiniteTimeSpan };

    public static async Task ProxyShapeRequest(HttpContext httpContext, IExecutionContext executionContext, IConfiguration configuration, Dictionary<string, ShapeTableConfig> allowedTables)
    {
        var tenantId = executionContext.TenantId;
        if (tenantId is null)
        {
            httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        var query = httpContext.Request.Query;
        var table = query["table"].ToString();

        if (string.IsNullOrEmpty(table) || !allowedTables.TryGetValue(table, out var tableConfig))
        {
            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        if (tableConfig.RequiredRole is not null && executionContext.UserInfo.Role != tableConfig.RequiredRole)
        {
            httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
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
            if (param.Key is not ("where" or "columns"))
            {
                upstreamParameters[param.Key] = param.Value.ToString();
            }
        }

        var whereClause = $"{tableConfig.TenantColumn}='{tenantId.Value}'";

        if (tableConfig.UserScopedColumn is not null)
        {
            var userId = executionContext.UserInfo.Id;
            if (userId is null)
            {
                httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }

            whereClause += $" AND {tableConfig.UserScopedColumn}='{userId.Value}'";
        }

        upstreamParameters["where"] = whereClause;
        upstreamParameters["columns"] = string.Join(",", tableConfig.Columns);

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
