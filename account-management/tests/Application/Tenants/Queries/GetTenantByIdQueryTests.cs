using NSubstitute;
using PlatformPlatform.AccountManagement.Application.Tenants.Queries;
using PlatformPlatform.AccountManagement.Domain.Tenants;

namespace PlatformPlatform.AccountManagement.Tests.Application.Tenants.Queries;

public class GetTenantByIdQueryTests
{
    [Fact]
    public async Task GetTenantByIdQuery_ShouldReturnTenantResponseDto_WhenTenantFound()
    {
        // Arrange
        var expectedTenantId = IdGenerator.NewId();
        var expectedTenantName = "TestTenant";

        var tenant = new Tenant {Id = expectedTenantId, Name = expectedTenantName};
        var tenantRepository = Substitute.For<ITenantRepository>();
        tenantRepository.GetByIdAsync(expectedTenantId, default).Returns(tenant);

        var query = new GetTenantByIdQuery(expectedTenantId);
        var handler = new GetTenantQueryHandler(tenantRepository);

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        result.Id.Should().Be(expectedTenantId);
        result.Name.Should().Be(expectedTenantName);
        await tenantRepository.Received().GetByIdAsync(expectedTenantId, default);
    }

    [Fact]
    public async Task GetTenantByIdQuery_ShouldThrowException_WhenTenantNotFound()
    {
        // Arrange
        long nonExistingTenantId = 999;

        var tenantRepository = Substitute.For<ITenantRepository>();
        tenantRepository.GetByIdAsync(nonExistingTenantId, default).Returns((Tenant?) null);

        var query = new GetTenantByIdQuery(nonExistingTenantId);
        var handler = new GetTenantQueryHandler(tenantRepository);

        // Act
        Func<Task> act = async () => await handler.Handle(query, default);

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("TenantNotFound");
        await tenantRepository.Received().GetByIdAsync(nonExistingTenantId, default);
    }
}