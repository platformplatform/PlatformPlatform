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
        const string expectedTenantName = "TestTenant";

        var tenant = new Tenant
        {
            Id = expectedTenantId,
            Name = expectedTenantName,
            Subdomain = "tenant1",
            Email = "foo@tenant1.com",
            Phone = "1234567890"
        };
        var tenantRepository = Substitute.For<ITenantRepository>();
        tenantRepository.GetByIdAsync(expectedTenantId, default).Returns(tenant);
        var handler = new GetTenantQueryHandler(tenantRepository);

        // Act
        var query = new GetTenantByIdQuery(expectedTenantId);
        var getTenantByIdQueryResult = await handler.Handle(query, default);

        // Assert
        getTenantByIdQueryResult.IsSuccess.Should().BeTrue();
        var tenantResponseDto = getTenantByIdQueryResult.Value;
        tenantResponseDto.Should().NotBeNull();
        tenantResponseDto.Id.Should().Be(expectedTenantId.AsRawString());
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
        var getTenantByIdQueryResult = await handler.Handle(query, default);

        // Assert
        getTenantByIdQueryResult.IsSuccess.Should().BeFalse();
        await tenantRepository.Received().GetByIdAsync(nonExistingTenantId, default);
    }
}