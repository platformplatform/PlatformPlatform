using FluentAssertions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PlatformPlatform.AccountManagement.Application;
using PlatformPlatform.AccountManagement.Domain;
using PlatformPlatform.AccountManagement.Domain.Tenants;
using PlatformPlatform.AccountManagement.Infrastructure;
using PlatformPlatform.AccountManagement.Infrastructure.Tenants;
using PlatformPlatform.Foundation.DomainModeling;
using Xunit;

namespace PlatformPlatform.AccountManagement.Tests.Infrastructure.Tenants;

public sealed class TenantRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _applicationDbContext;
    private readonly SqliteInMemoryDbContextFactory<ApplicationDbContext> _dbContextFactory;
    private readonly TenantRepository _tenantRepository;

    public TenantRepositoryTests()
    {
        var services = new ServiceCollection();

        _dbContextFactory = new SqliteInMemoryDbContextFactory<ApplicationDbContext>();
        _applicationDbContext = _dbContextFactory.CreateContext();
        services.AddDomainModelingServices(ApplicationConfiguration.Assembly, DomainConfiguration.Assembly);

        _tenantRepository = new TenantRepository(_applicationDbContext);
    }

    public void Dispose()
    {
        _dbContextFactory.Dispose();
    }

    [Fact]
    public async Task Add_WhenTenantDoesNotExist_ShouldAddTenantToDatabase()
    {
        // Arrange
        var tenant = Tenant.Create("New Tenant", "new", "new@test.com", "1234567890");

        // Act
        _tenantRepository.Add(tenant);
        await _applicationDbContext.SaveChangesAsync();

        // Assert
        var retrievedTenant = await _applicationDbContext.Tenants.FirstOrDefaultAsync(t => t.Id == tenant.Id);
        retrievedTenant.Should().NotBeNull();
        retrievedTenant!.Id.Should().Be(tenant.Id);
    }

    [Fact]
    public async Task Update_WhenTenantExists_ShouldUpdateTenantInDatabase()
    {
        // Arrange
        var tenant = Tenant.Create("Existing Tenant", "existing", "existing@test.com", "1234567890");
        await _applicationDbContext.Tenants.AddAsync(tenant);
        await _applicationDbContext.SaveChangesAsync();

        // Act
        tenant.Update("Updated Tenant", "existing@test.com", "1234567890");
        _tenantRepository.Update(tenant);
        await _applicationDbContext.SaveChangesAsync();

        // Assert
        var updatedTenant = await _applicationDbContext.Tenants.FirstOrDefaultAsync(t => t.Id == tenant.Id);
        updatedTenant.Should().NotBeNull();
        updatedTenant!.Name.Should().Be("Updated Tenant");
    }

    [Fact]
    public async Task Remove_WhenTenantExists_ShouldRemoveTenantFromDatabase()
    {
        // Arrange
        var tenant = Tenant.Create("Existing Tenant", "existing", "existing@test.com", "1234567890");
        await _applicationDbContext.Tenants.AddAsync(tenant);
        await _applicationDbContext.SaveChangesAsync();

        // Act
        _tenantRepository.Remove(tenant);
        await _applicationDbContext.SaveChangesAsync();

        // Assert
        var removedTenant = await _applicationDbContext.Tenants.FirstOrDefaultAsync(t => t.Id == tenant.Id);
        removedTenant.Should().BeNull();
    }

    [Fact]
    public async Task IsSubdomainFreeAsync_WhenSubdomainAlreadyExists_ShouldReturnFalse()
    {
        // Arrange  
        var tenant = Tenant.Create("Existing Tenant", "existing", "existing@test.com", "1234567890");

        await _applicationDbContext.Tenants.AddAsync(tenant);
        await _applicationDbContext.SaveChangesAsync();

        // Act
        var isSubdomainFree = await _tenantRepository.IsSubdomainFreeAsync("existing", CancellationToken.None);

        // Assert
        isSubdomainFree.Should().BeFalse();
    }

    [Fact]
    public async Task IsSubdomainFreeAsync_WhenSubdomainDoesNotExist_ShouldReturnTrue()
    {
        // Arrange
        var tenant = Tenant.Create("Existing Tenant", "existing", "existing@test.com", "1234567890");

        await _applicationDbContext.Tenants.AddAsync(tenant);
        await _applicationDbContext.SaveChangesAsync();

        // Act
        var isSubdomainFree = await _tenantRepository.IsSubdomainFreeAsync("nonexistent", CancellationToken.None);

        // Assert
        isSubdomainFree.Should().BeTrue();
    }

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
    public async Task Create_WhenInvalidProperties_ShouldThrowException(string name, string subdomain, string email,
        string phone)
    {
        // Arrange
        var tenant = Tenant.Create(name, subdomain, email, phone);
        await _applicationDbContext.Tenants.AddAsync(tenant);

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() =>
        {
            _applicationDbContext.SaveChangesAsync();

            return Task.CompletedTask;
        });
    }
}