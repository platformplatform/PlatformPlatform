using FluentAssertions;
using NSubstitute;
using PlatformPlatform.AccountManagement.Application.Tenants.Queries;
using PlatformPlatform.AccountManagement.Domain.Tenants;
using Xunit;

namespace PlatformPlatform.AccountManagement.Tests.Application.Tenants.Queries;

public class GetTenantByIdQueryTests
{
    [Fact]
    public async Task GetTenantByIdQuery_WhenTenantFound_ShouldReturnTenantResponseDto()
    {
        // Arrange
        var expectedTenantId = TenantId.NewId();
        var expectedTenantName = "TestTenant";

        var tenant = new Tenant {Id = expectedTenantId, Name = expectedTenantName};
        var tenantRepository = Substitute.For<ITenantRepository>();
        tenantRepository.GetByIdAsync(expectedTenantId, default).Returns(tenant);
        var handler = new GetTenantQueryHandler(tenantRepository);

        // Act
        var query = new GetTenantByIdQuery(expectedTenantId);
        var tenantResponseDto = await handler.Handle(query, default);

        // Assert
        tenantResponseDto.Should().NotBeNull();
        tenantResponseDto!.Id.Should().Be(expectedTenantId.AsRawString());
        tenantResponseDto.Name.Should().Be(expectedTenantName);
        await tenantRepository.Received().GetByIdAsync(expectedTenantId, default);
    }

    [Fact]
    public async Task GetTenantByIdQuery_WhenTenantNotFound_ShouldReturnNull()
    {
        // Arrange
        var nonExistingTenantId = new TenantId(999);

        var tenantRepository = Substitute.For<ITenantRepository>();
        tenantRepository.GetByIdAsync(nonExistingTenantId, default).Returns((Tenant?) null);
        var handler = new GetTenantQueryHandler(tenantRepository);

        // Act
        var query = new GetTenantByIdQuery(nonExistingTenantId);
        var tenantResponseDto = await handler.Handle(query, default);

        // Assert
        tenantResponseDto.Should().BeNull();
        await tenantRepository.Received().GetByIdAsync(nonExistingTenantId, default);
    }
}