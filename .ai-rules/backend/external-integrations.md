# External Integrations

When implementing integration to external services, follow these rules very carefully.

## Structure

Integration to external services should be implemented as a client in a file located at `/[scs-name]/Core/Integrations/[ServiceName]/[ServiceClient].cs`.

## Implementation

1. Create a client class with a clear purpose and name.
2. Implement proper error handling and logging.
3. Return appropriate types (null, optional, or Result types) rather than throwing exceptions.
4. Use typed clients with HttpClient injection (via AddHttpClient<T>) for HTTP-based integrations.
5. Consider timeouts, retry policies, and circuit breakers for resilience.
6. Create DTOs for request and response data when needed (but don't postfix with `Dto`).
7. Keep implementation in one file.

## Example - Gravatar Integration

```csharp
public sealed record Gravatar(Stream Stream, string ContentType);

public sealed class GravatarClient(HttpClient httpClient, ILogger<GravatarClient> logger)
{
    public async Task<Gravatar?> GetGravatar(UserId userId, string email, CancellationToken cancellationToken)
    {
        try
        {
            var hash = Convert.ToHexString(MD5.HashData(Encoding.ASCII.GetBytes(email)));
            var gravatarUrl = $"avatar/{hash.ToLowerInvariant()}?d=404";

            var response = await httpClient.GetAsync(gravatarUrl, cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                logger.LogInformation("No Gravatar found for user {UserId}", userId);
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("Failed to fetch Gravatar for user {UserId}. Status Code: {StatusCode}", userId, response.StatusCode);
                return null;
            }

            return new Gravatar(
                await response.Content.ReadAsStreamAsync(cancellationToken),
                response.Content.Headers.ContentType?.MediaType!
            );
        }
        catch (TaskCanceledException ex)
        {
            logger.LogError(ex, "Timeout when fetching gravatar for user {UserId}", userId);
            return null;
        }
    }
}
```

## Key Implementation Details

1. Constructor Injection: Uses constructor injection with primary constructor syntax for dependencies.
2. Typed Client Pattern: Uses the HttpClient directly via constructor injection.
3. Error Handling: Properly handles HTTP status codes and avoid throwing exceptions.
4. Logging: Logs information and errors with structured logging.
5. Return Type: Returns null when the resource is not found or an error occurs.
6. Cancellation Support: Accepts and passes through a CancellationToken.

## Registration

Register the client in the DI container using the typed client pattern:

```csharp
services.AddHttpClient<GravatarClient>(client =>
{
    client.BaseAddress = new Uri("https://gravatar.com/");
    client.Timeout = TimeSpan.FromSeconds(5);
});
```

For additional resilience, you can add Polly policies (requires `Microsoft.Extensions.Http.Polly` package):

```csharp
services.AddHttpClient<GravatarClient>(client =>
{
    client.BaseAddress = new Uri("https://gravatar.com/");
    client.Timeout = TimeSpan.FromSeconds(5);
})
.AddTransientHttpErrorPolicy(policy => policy.WaitAndRetryAsync(
    new[] { TimeSpan.FromMilliseconds(500), TimeSpan.FromSeconds(1) }
));
```
