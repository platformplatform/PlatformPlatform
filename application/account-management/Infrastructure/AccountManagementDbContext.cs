using Microsoft.EntityFrameworkCore;
using PlatformPlatform.AccountManagement.Domain.AccountRegistrations;
using PlatformPlatform.SharedKernel.InfrastructureCore.EntityFramework;

namespace PlatformPlatform.AccountManagement.Infrastructure;

public sealed class AccountManagementDbContext(DbContextOptions<AccountManagementDbContext> options)
    : SharedKernelDbContext<AccountManagementDbContext>(options)
{
    public DbSet<AccountRegistration> AccountRegistrations => Set<AccountRegistration>();

    public DbSet<Tenant> Tenants => Set<Tenant>();

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // AccountRegistration
        modelBuilder.MapStronglyTypedUuid<AccountRegistration, AccountRegistrationId>(a => a.Id);
        modelBuilder.MapStronglyTypedNullableId<AccountRegistration, TenantId, string>(u => u.TenantId);

        // Tenant
        modelBuilder.MapStronglyTypedId<Tenant, TenantId, string>(t => t.Id);

        // User
        modelBuilder.MapStronglyTypedUuid<User, UserId>(u => u.Id);
        modelBuilder.MapStronglyTypedId<User, TenantId, string>(u => u.TenantId);
        modelBuilder.Entity<User>()
            .HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(u => u.TenantId)
            .HasPrincipalKey(t => t.Id);
    }
}