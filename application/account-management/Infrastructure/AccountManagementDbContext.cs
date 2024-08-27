using Microsoft.EntityFrameworkCore;
using PlatformPlatform.AccountManagement.Domain.Authentication;
using PlatformPlatform.AccountManagement.Domain.Signups;
using PlatformPlatform.SharedKernel.InfrastructureCore.EntityFramework;

namespace PlatformPlatform.AccountManagement.Infrastructure;

public sealed class AccountManagementDbContext(DbContextOptions<AccountManagementDbContext> options)
    : SharedKernelDbContext<AccountManagementDbContext>(options)
{
    public DbSet<Signup> Signups => Set<Signup>();

    public DbSet<Login> Logins => Set<Login>();

    public DbSet<Tenant> Tenants => Set<Tenant>();

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Signup
        modelBuilder.MapStronglyTypedUuid<Signup, SignupId>(a => a.Id);
        modelBuilder.MapStronglyTypedNullableId<Signup, TenantId, string>(u => u.TenantId);

        // Login
        modelBuilder.MapStronglyTypedId<Login, LoginId, string>(t => t.Id);
        modelBuilder.MapStronglyTypedId<Login, TenantId, string>(u => u.TenantId);
        modelBuilder.MapStronglyTypedUuid<Login, UserId>(u => u.UserId);

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
