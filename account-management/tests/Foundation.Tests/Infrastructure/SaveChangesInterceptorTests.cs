using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PlatformPlatform.Foundation.Infrastructure;
using PlatformPlatform.Foundation.Tests.Domain;
using Xunit;

namespace PlatformPlatform.Foundation.Tests.Infrastructure;

public class SaveChangesInterceptorTests
{
    private readonly TestDbContext _testDbContext;

    public SaveChangesInterceptorTests()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase("TenantRepositoryTests")
            .Options;
        _testDbContext = new TestDbContext(options);
    }

    [Fact]
    public async Task SavingChangesAsync_WhenEntityIsAdded_ShouldSetCreatedAt()
    {
        // Arrange
        var newTenant = TestAggregate.Create("TestTenant");

        // Act
        _testDbContext.Tenants.Add(newTenant);
        await _testDbContext.SaveChangesAsync();

        // Assert
        newTenant.CreatedAt.Should().NotBe(default);
        newTenant.ModifiedAt.Should().BeNull();
    }

    [Fact]
    public async Task SavingChangesAsync_WhenEntityIsModified_ShouldUpdateModifiedAt()
    {
        // Arrange
        var newTenant = TestAggregate.Create("TestTenant");
        _testDbContext.Tenants.Add(newTenant);
        await _testDbContext.SaveChangesAsync();
        var initialCreatedAt = newTenant.CreatedAt;
        var initialModifiedAt = newTenant.ModifiedAt;

        // Act
        newTenant.Name = "UpdatedTenant";
        await _testDbContext.SaveChangesAsync();

        // Assert
        newTenant.ModifiedAt.Should().NotBe(default);
        newTenant.ModifiedAt.Should().NotBe(initialModifiedAt);
        newTenant.CreatedAt.Should().Be(initialCreatedAt);
    }
}

public class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
    {
    }

    public DbSet<TestAggregate> Tenants => Set<TestAggregate>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        DbContextConfiguration.ConfigureOnModelCreating(modelBuilder);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        DbContextConfiguration.ConfigureOnConfiguring(optionsBuilder);
    }
}

public static class DbContextConfiguration
{
    public static void ConfigureOnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseStringForEnums();
    }

    public static void ConfigureOnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.AddInterceptors(new UpdateAuditableEntitiesInterceptor());
    }
}