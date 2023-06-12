using FluentAssertions;
using NSubstitute;
using PlatformPlatform.AccountManagement.Application.Tenants;
using PlatformPlatform.AccountManagement.Domain.Tenants;
using PlatformPlatform.AccountManagement.Infrastructure;
using Xunit;

namespace PlatformPlatform.AccountManagement.Tests.Application.Tenants;

public class GetTenantTests : BaseTest<AccountManagementDbContext>
{
    [Fact]
    public async Task GetTenant_WhenTenantFound_ShouldReturnTenantResponseDto()
    {
        // Arrange
        var expectedTenantId = new TenantId("tenant1");
        const string expectedTenantName = "TestTenant";

        var tenant = new Tenant(expectedTenantId, expectedTenantName, "1234567890");

        var tenantRepository = Substitute.For<ITenantRepository>();
        tenantRepository.GetByIdAsync(expectedTenantId, default).Returns(tenant);
        var handler = new GetTenant.Handler(tenantRepository);

        // Act
        var query = new GetTenant.Query(expectedTenantId);
        var result = await handler.Handle(query, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var tenantResponseDto = result.Value;
        tenantResponseDto.Should().NotBeNull();
        tenantResponseDto!.Id.Should().Be(expectedTenantId.ToString());
        tenantResponseDto.Name.Should().Be(expectedTenantName);
        await tenantRepository.Received().GetByIdAsync(expectedTenantId, default);
    }

    [Fact]
    public async Task GetTenantQuery_WhenTenantNotFound_ShouldReturnNull()
    {
        // Arrange
        var nonExistingTenantId = new TenantId("unknown");

        var tenantRepository = Substitute.For<ITenantRepository>();
        tenantRepository.GetByIdAsync(nonExistingTenantId, default).Returns((Tenant?) null);
        var handler = new GetTenant.Handler(tenantRepository);

        // Act
        var query = new GetTenant.Query(nonExistingTenantId);
        var result = await handler.Handle(query, default);

        // Assert
        result.IsSuccess.Should().BeFalse();
        await tenantRepository.Received().GetByIdAsync(nonExistingTenantId, default);
    }
}