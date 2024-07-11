using Microsoft.EntityFrameworkCore;
using PlatformPlatform.AccountManagement.Api.AccountRegistrations.Domain;
using PlatformPlatform.AccountManagement.Api.Tenants.Domain;
using PlatformPlatform.AccountManagement.Api.Users.Domain;
using PlatformPlatform.SharedKernel.Infrastructure.EntityFramework;

namespace PlatformPlatform.AccountManagement.Api;

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
            .OwnsOne(e => e.Avatar, b => b.ToJson())
            .HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(u => u.TenantId)
            .HasPrincipalKey(t => t.Id);
    }
}
