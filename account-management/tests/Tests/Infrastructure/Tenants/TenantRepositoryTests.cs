using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PlatformPlatform.AccountManagement.Domain.Tenants;
using PlatformPlatform.AccountManagement.Infrastructure;
using PlatformPlatform.AccountManagement.Infrastructure.Tenants;
using Xunit;

namespace PlatformPlatform.AccountManagement.Tests.Infrastructure.Tenants;

public class TenantRepositoryTests
{
    private readonly ApplicationDbContext _applicationDbContext;
    private readonly ITenantRepository _tenantRepository;

    public TenantRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase("TenantRepositoryTests")
            .Options;
        _applicationDbContext = new ApplicationDbContext(options);
        _tenantRepository = new TenantRepository(_applicationDbContext);
    }

    [Fact]
    public async Task IsSubdomainFreeAsync_WhenSubdomainAlreadyExists_ShouldReturnFalse()
    {
        // Arrange
        var tenant = new Tenant
        {
            Name = "Existing Tenant",
            Subdomain = "existing",
            Email = "existing@test.com",
            Phone = "1234567890"
        };

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
        var tenant = new Tenant
        {
            Name = "Existing Tenant",
            Subdomain = "existing",
            Email = "existing@test.com",
            Phone = "1234567890"
        };

        await _applicationDbContext.Tenants.AddAsync(tenant);
        await _applicationDbContext.SaveChangesAsync();

        // Act
        var isSubdomainFree = await _tenantRepository.IsSubdomainFreeAsync("nonexistent", CancellationToken.None);

        // Assert
        isSubdomainFree.Should().BeTrue();
    }
}