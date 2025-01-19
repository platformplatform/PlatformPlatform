using Microsoft.EntityFrameworkCore;
using PlatformPlatform.AccountManagement.Features.Authentication.Domain;
using PlatformPlatform.AccountManagement.Features.Signups.Domain;
using PlatformPlatform.AccountManagement.Features.Tenants.Domain;
using PlatformPlatform.AccountManagement.Features.Users.Domain;
using PlatformPlatform.SharedKernel.EntityFramework;
using PlatformPlatform.SharedKernel.ExecutionContext;

namespace PlatformPlatform.AccountManagement.Database;

public sealed class AccountManagementDbContext(DbContextOptions<AccountManagementDbContext> options, IExecutionContext executionContext)
    : SharedKernelDbContext<AccountManagementDbContext>(options, executionContext)
{
    public DbSet<Login> Logins => Set<Login>();

    public DbSet<Signup> Signups => Set<Signup>();

    public DbSet<Tenant> Tenants => Set<Tenant>();

    public DbSet<User> Users => Set<User>();
}
