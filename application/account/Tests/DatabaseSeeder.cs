using System.Net;
using PlatformPlatform.Account.Database;
using PlatformPlatform.Account.Features.Authentication.Domain;
using PlatformPlatform.Account.Features.Subscriptions.Domain;
using PlatformPlatform.Account.Features.Tenants.Domain;
using PlatformPlatform.Account.Features.Users.Domain;

namespace PlatformPlatform.Account.Tests;

public sealed class DatabaseSeeder
{
    public readonly Tenant Tenant1;
    public readonly User Tenant1Member;
    public readonly Session Tenant1MemberSession;
    public readonly User Tenant1Owner;
    public readonly Session Tenant1OwnerSession;
    public readonly Subscription Tenant1Subscription;

    public DatabaseSeeder(AccountDbContext accountDbContext)
    {
        Tenant1 = Tenant.Create("owner@tenant-1.com");
        accountDbContext.Set<Tenant>().AddRange(Tenant1);

        Tenant1Owner = User.Create(Tenant1.Id, "owner@tenant-1.com", UserRole.Owner, true, null);
        accountDbContext.Set<User>().AddRange(Tenant1Owner);

        Tenant1Member = User.Create(Tenant1.Id, "member1@tenant-1.com", UserRole.Member, true, null);
        accountDbContext.Set<User>().AddRange(Tenant1Member);

        Tenant1OwnerSession = Session.Create(Tenant1.Id, Tenant1Owner.Id, LoginMethod.OneTimePassword, "TestUserAgent", IPAddress.Loopback);
        accountDbContext.Set<Session>().AddRange(Tenant1OwnerSession);

        Tenant1MemberSession = Session.Create(Tenant1.Id, Tenant1Member.Id, LoginMethod.OneTimePassword, "TestUserAgent", IPAddress.Loopback);
        accountDbContext.Set<Session>().AddRange(Tenant1MemberSession);

        Tenant1Subscription = Subscription.Create(Tenant1.Id);
        accountDbContext.Set<Subscription>().Add(Tenant1Subscription);

        accountDbContext.SaveChanges();
    }
}
