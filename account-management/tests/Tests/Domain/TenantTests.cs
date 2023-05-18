using PlatformPlatform.AccountManagement.Domain.Tenants;
using Xunit;

namespace PlatformPlatform.AccountManagement.Tests.Domain;

public class TenantTests
{
    [Theory]
    [InlineData("To long phone number", "tenant1", "foo@tenant1.com", "0099 (999) 888-77-66-55")]
    [InlineData("Invalid phone number", "tenant1", "foo@tenant1.com", "N/A")]
    [InlineData("", "notenantname", "foo@tenant1.com", "1234567890")]
    [InlineData("Too long tenant name above 30 characters", "tenant1", "foo@tenant1.com", "+55 (21) 99999-9999")]
    [InlineData("No email", "tenant1", "", "+61 2 1234 5678")]
    [InlineData("Invalid Email", "tenant1", "@tenant1.com", "1234567890")]
    [InlineData("No subdomain", "", "foo@tenant1.com", "1234567890")]
    [InlineData("To short subdomain", "ab", "foo@tenant1.com", "1234567890")]
    [InlineData("To long subdomain", "1234567890123456789012345678901", "foo@tenant1.com", "1234567890")]
    [InlineData("Subdomain with uppercase", "Tenant1", "foo@tenant1.com", "1234567890")]
    [InlineData("Subdomain special characters", "tenant-1", "foo@tenant1.com", "1234567890")]
    [InlineData("Subdomain with spaces", "tenant 1", "foo@tenant1.com", "1234567890")]
    public void Create_WhenInvalidProperties_ShouldThrowException(string name, string subdomain, string email,
        string phone)
    {
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => Tenant.Create(name, subdomain, email, phone));
    }
}