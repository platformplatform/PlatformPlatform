using System.Net;
using PlatformPlatform.AccountManagement.Database;
using PlatformPlatform.AccountManagement.Features.Authentication.Domain;
using PlatformPlatform.AccountManagement.Features.Tenants.Domain;
using PlatformPlatform.AccountManagement.Features.Users.Domain;

namespace PlatformPlatform.AccountManagement.Tests;

public sealed class DatabaseSeeder
{
    public readonly Tenant Tenant1;
    public readonly User Tenant1Member;
    public readonly Session Tenant1MemberSession;
    public readonly User Tenant1Owner;
    public readonly Session Tenant1OwnerSession;

    public DatabaseSeeder(AccountManagementDbContext accountManagementDbContext)
    {
        Tenant1 = Tenant.Create("owner@tenant-1.com");
        accountManagementDbContext.Set<Tenant>().AddRange(Tenant1);

        Tenant1Owner = User.Create(Tenant1.Id, "owner@tenant-1.com", UserRole.Owner, true, null);
        accountManagementDbContext.Set<User>().AddRange(Tenant1Owner);

        Tenant1Member = User.Create(Tenant1.Id, "member1@tenant-1.com", UserRole.Member, true, null);
        accountManagementDbContext.Set<User>().AddRange(Tenant1Member);

        Tenant1OwnerSession = Session.Create(Tenant1.Id, Tenant1Owner.Id, LoginMethod.OneTimePassword, "TestUserAgent", IPAddress.Loopback);
        accountManagementDbContext.Set<Session>().AddRange(Tenant1OwnerSession);

        Tenant1MemberSession = Session.Create(Tenant1.Id, Tenant1Member.Id, LoginMethod.OneTimePassword, "TestUserAgent", IPAddress.Loopback);
        accountManagementDbContext.Set<Session>().AddRange(Tenant1MemberSession);

        accountManagementDbContext.SaveChanges();
    }
}
