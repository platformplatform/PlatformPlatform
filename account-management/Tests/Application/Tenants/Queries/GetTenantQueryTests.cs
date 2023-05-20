using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using PlatformPlatform.AccountManagement.Application;
using PlatformPlatform.AccountManagement.Application.Tenants.Queries;
using PlatformPlatform.AccountManagement.Domain.Tenants;
using Xunit;

namespace PlatformPlatform.AccountManagement.Tests.Application.Tenants.Queries;

public class GetTenantQueryTests
{
    public GetTenantQueryTests()
    {
        var services = new ServiceCollection();
        services.AddApplicationServices();
    }

    [Fact]
    public async Task GetTenantQuery_WhenTenantFound_ShouldReturnTenantResponseDto()
    {
        // Arrange
        var expectedTenantId = TenantId.NewId();
        const string expectedTenantName = "TestTenant";

        var tenant = new Tenant(expectedTenantName, "foo@tenant1.com", "1234567890")
        {
            Id = expectedTenantId,
            Subdomain = "tenant1"
        };
        var tenantRepository = Substitute.For<ITenantRepository>();
        tenantRepository.GetByIdAsync(expectedTenantId, default).Returns(tenant);
        var handler = new GetTenantQueryHandler(tenantRepository);

        // Act
        var query = new GetTenantQuery(expectedTenantId);
        var getTenantQueryResult = await handler.Handle(query, default);

        // Assert
        getTenantQueryResult.IsSuccess.Should().BeTrue();
        var tenantResponse = getTenantQueryResult.Value;
        tenantResponse.Should().NotBeNull();
        tenantResponse.Id.Should().Be(expectedTenantId);
        tenantResponse.Name.Should().Be(expectedTenantName);
        await tenantRepository.Received().GetByIdAsync(expectedTenantId, default);
    }

    [Fact]
    public async Task GetTenantQuery_WhenTenantNotFound_ShouldReturnNull()
    {
        // Arrange
        var nonExistingTenantId = new TenantId(999);

        var tenantRepository = Substitute.For<ITenantRepository>();
        tenantRepository.GetByIdAsync(nonExistingTenantId, default).Returns((Tenant?) null);
        var handler = new GetTenantQueryHandler(tenantRepository);

        // Act
        var query = new GetTenantQuery(nonExistingTenantId);
        var getTenantQueryResult = await handler.Handle(query, default);

        // Assert
        getTenantQueryResult.IsSuccess.Should().BeFalse();
        await tenantRepository.Received().GetByIdAsync(nonExistingTenantId, default);
    }
}