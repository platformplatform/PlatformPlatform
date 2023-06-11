using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PlatformPlatform.AccountManagement.Application;
using PlatformPlatform.AccountManagement.Domain.Tenants;
using PlatformPlatform.AccountManagement.Infrastructure;
using PlatformPlatform.AccountManagement.Infrastructure.Tenants;
using PlatformPlatform.SharedKernel.ApplicationCore;
using Xunit;

namespace PlatformPlatform.AccountManagement.Tests.Infrastructure.Tenants;

public sealed class TenantRepositoryTests : IDisposable
{
    private readonly AccountManagementDbContext _accountManagementDbContext;
    private readonly SqliteInMemoryDbContextFactory<AccountManagementDbContext> _dbContextFactory;
    private readonly TenantRepository _tenantRepository;

    public TenantRepositoryTests()
    {
        var services = new ServiceCollection();

        _dbContextFactory = new SqliteInMemoryDbContextFactory<AccountManagementDbContext>();
        _accountManagementDbContext = _dbContextFactory.CreateContext();
        services.AddApplicationServices(ApplicationConfiguration.Assembly);

        _tenantRepository = new TenantRepository(_accountManagementDbContext);
    }

    public void Dispose()
    {
        _dbContextFactory.Dispose();
    }

    [Fact]
    public async Task Add_WhenTenantDoesNotExist_ShouldAddTenantToDatabase()
    {
        // Arrange
        var tenant = Tenant.Create("new", "New Tenant", "1234567890");
        var cancellationToken = new CancellationToken();

        // Act
        await _tenantRepository.AddAsync(tenant, cancellationToken);
        await _accountManagementDbContext.SaveChangesAsync(cancellationToken);

        // Assert
        var retrievedTenant =
            await _accountManagementDbContext.Tenants.FirstOrDefaultAsync(t => t.Id == tenant.Id, cancellationToken);
        retrievedTenant.Should().NotBeNull();
        retrievedTenant!.Id.Should().Be(tenant.Id);
    }

    [Fact]
    public async Task Update_WhenTenantExists_ShouldUpdateTenantInDatabase()
    {
        // Arrange
        var tenant = Tenant.Create("existing", "Existing Tenant", "1234567890");
        await _accountManagementDbContext.Tenants.AddAsync(tenant);
        await _accountManagementDbContext.SaveChangesAsync();

        // Act
        tenant.Update("Updated Tenant", "1234567890");
        _tenantRepository.Update(tenant);
        await _accountManagementDbContext.SaveChangesAsync();

        // Assert
        var updatedTenant = await _accountManagementDbContext.Tenants.FirstOrDefaultAsync(t => t.Id == tenant.Id);
        updatedTenant.Should().NotBeNull();
        updatedTenant!.Name.Should().Be("Updated Tenant");
    }

    [Fact]
    public async Task Remove_WhenTenantExists_ShouldRemoveTenantFromDatabase()
    {
        // Arrange
        var tenant = Tenant.Create("existing", "Existing Tenant", "1234567890");
        await _accountManagementDbContext.Tenants.AddAsync(tenant);
        await _accountManagementDbContext.SaveChangesAsync();

        // Act
        _tenantRepository.Remove(tenant);
        await _accountManagementDbContext.SaveChangesAsync();

        // Assert
        var removedTenant = await _accountManagementDbContext.Tenants.FirstOrDefaultAsync(t => t.Id == tenant.Id);
        removedTenant.Should().BeNull();
    }

    [Fact]
    public async Task IsSubdomainFreeAsync_WhenSubdomainAlreadyExists_ShouldReturnFalse()
    {
        // Arrange  
        var tenant = Tenant.Create("existing", "Existing Tenant", "1234567890");

        await _accountManagementDbContext.Tenants.AddAsync(tenant);
        await _accountManagementDbContext.SaveChangesAsync();

        // Act
        var isSubdomainFree = await _tenantRepository.IsSubdomainFreeAsync("existing", CancellationToken.None);

        // Assert
        isSubdomainFree.Should().BeFalse();
    }

    [Fact]
    public async Task IsSubdomainFreeAsync_WhenSubdomainDoesNotExist_ShouldReturnTrue()
    {
        // Arrange
        var tenant = Tenant.Create("existing", "Existing Tenant", "1234567890");

        await _accountManagementDbContext.Tenants.AddAsync(tenant);
        await _accountManagementDbContext.SaveChangesAsync();

        // Act
        var isSubdomainFree = await _tenantRepository.IsSubdomainFreeAsync("nonexistent", CancellationToken.None);

        // Assert
        isSubdomainFree.Should().BeTrue();
    }
}