using System.Text.Json;
using FluentAssertions;
using PlatformPlatform.SharedKernel.Domain;
using Xunit;

namespace PlatformPlatform.SharedKernel.Tests.Domain;

public sealed class TenantIdTests
{
    [Fact]
    public void TenantId_WhenSerializedToJson_DoesNotGenerateInternalValueProperty()
    {
        // Arrange
        const string tenantDomain = "tenant";
        const string email = "me@example.com";
        var tenantObject = new TenantObject(new TenantId(tenantDomain), email);

        // Act
        var json = JsonSerializer.Serialize(tenantObject);

        // Assert
        const string expectedJson = $"{{\"Id\":\"{tenantDomain}\",\"Email\":\"{email}\"}}";
        json.Should().Be(expectedJson);
    }

    [Fact]
    public void TenantId_WhenSerializedToJsonAndDeserializing_CreatesSameResult()
    {
        // Arrange
        const string tenantDomain = "tenant";
        const string email = "me@example.com";
        var tenantObject = new TenantObject(new TenantId(tenantDomain), email);

        // Act
        var json = JsonSerializer.Serialize(tenantObject);
        var actual = JsonSerializer.Deserialize<TenantObject>(json);

        // Assert
        actual.Should().BeEquivalentTo(tenantObject);
    }

    public sealed record TenantObject(TenantId Id, string Email);
}
