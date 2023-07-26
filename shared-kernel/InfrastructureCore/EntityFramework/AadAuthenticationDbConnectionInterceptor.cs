using System.Data.Common;
using Azure.Core;
using Azure.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace PlatformPlatform.SharedKernel.InfrastructureCore.EntityFramework;

public class AadAuthenticationDbConnectionInterceptor : DbConnectionInterceptor
{
    public override async ValueTask<InterceptionResult> ConnectionOpeningAsync(DbConnection connection,
        ConnectionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
    {
        var sqlConnection = (SqlConnection) connection;

        var connectionStringBuilder = new SqlConnectionStringBuilder(sqlConnection.ConnectionString);
        if (connectionStringBuilder.DataSource.Contains("database.windows.net", StringComparison.OrdinalIgnoreCase) &&
            string.IsNullOrEmpty(connectionStringBuilder.UserID))
        {
            sqlConnection.AccessToken = await GetAzureManagedIdentitySqlAccessToken(cancellationToken);
        }

        return await base.ConnectionOpeningAsync(connection, eventData, result, cancellationToken);
    }

    private static async Task<string> GetAzureManagedIdentitySqlAccessToken(CancellationToken cancellationToken)
    {
        var managedIdentityClientId = Environment.GetEnvironmentVariable("MANAGED_IDENTITY_CLIENT_ID")
                                      ?? throw new Exception(
                                          "Missing MANAGED_IDENTITY_CLIENT_ID environment variable.");

        var defaultAzureCredentialOptions = new DefaultAzureCredentialOptions
            {ManagedIdentityClientId = managedIdentityClientId};
        var tokenRequestContext = new TokenRequestContext(new[] {"https://database.windows.net//.default"});
        var tokenRequestResult = await new DefaultAzureCredential(defaultAzureCredentialOptions)
            .GetTokenAsync(tokenRequestContext, cancellationToken);
        return tokenRequestResult.Token;
    }
}