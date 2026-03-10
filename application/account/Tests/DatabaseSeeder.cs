using System.Net;
using Account.Database;
using Account.Features.Authentication.Domain;
using Account.Features.FeatureFlags.Domain;
using Account.Features.Subscriptions.Domain;
using Account.Features.Tenants.Domain;
using Account.Features.Users.Domain;

namespace Account.Tests;

public sealed class DatabaseSeeder
{
    public readonly FeatureFlag BetaFeaturesFlag;
    public readonly FeatureFlag CompactViewFlag;
    public readonly FeatureFlag CustomBrandingFlag;
    public readonly FeatureFlag SsoFlag;
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

        var now = DateTimeOffset.UtcNow;

        BetaFeaturesFlag = FeatureFlag.Create("beta-features");
        BetaFeaturesFlag.Activate(now);
        accountDbContext.Set<FeatureFlag>().Add(BetaFeaturesFlag);

        SsoFlag = FeatureFlag.Create("sso");
        accountDbContext.Set<FeatureFlag>().Add(SsoFlag);

        CustomBrandingFlag = FeatureFlag.Create("custom-branding");
        CustomBrandingFlag.Activate(now);
        accountDbContext.Set<FeatureFlag>().Add(CustomBrandingFlag);

        CompactViewFlag = FeatureFlag.Create("compact-view");
        CompactViewFlag.Activate(now);
        accountDbContext.Set<FeatureFlag>().Add(CompactViewFlag);

        accountDbContext.SaveChanges();
    }
}
