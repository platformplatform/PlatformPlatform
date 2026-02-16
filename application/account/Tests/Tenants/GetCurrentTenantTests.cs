using FluentAssertions;
using NJsonSchema;
using PlatformPlatform.Account.Database;
using PlatformPlatform.SharedKernel.Tests;
using Xunit;

namespace PlatformPlatform.Account.Tests.Tenants;

public sealed class GetCurrentTenantTests : EndpointBaseTest<AccountDbContext>
{
    [Fact]
    public async Task GetCurrentTenant_WhenTenantExists_ShouldReturnTenantWithValidContract()
    {
        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync("/api/account/tenants/current");

        // Assert
        response.ShouldBeSuccessfulGetRequest();

        var schema = await JsonSchema.FromJsonAsync(
            """
            {
                'type': 'object',
                'properties': {
                    'id': {'type': 'string', 'pattern': '^[A-Z0-9]{19}$'},
                    'createdAt': {'type': 'string', 'format': 'date-time'},
                    'modifiedAt': {'type': ['null', 'string'], 'format': 'date-time'},
                    'name': {'type': 'string', 'minLength': 0, 'maxLength': 30},
                    'state': {'type': 'string', 'minLength': 1, 'maxLength':20},
                    'suspensionReason': {'type': ['null', 'string']},
                    'logoUrl': {'type': ['null', 'string']}
                },
                'required': ['id', 'createdAt', 'modifiedAt', 'name', 'state', 'suspensionReason', 'logoUrl'],
                'additionalProperties': false
            }
            """
        );

        var responseBody = await response.Content.ReadAsStringAsync();
        schema.Validate(responseBody).Should().BeEmpty();
    }
}
