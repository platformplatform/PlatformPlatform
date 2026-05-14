using System.Net;
using Account.Database;
using Account.Features.Authentication.Domain;
using Account.Features.FeatureFlags.Domain;
using Account.Features.Subscriptions.Domain;
using Account.Features.Tenants.Domain;
using Account.Features.Users.Domain;
using SharedKernel.FeatureFlags;

namespace Account.Tests;

public sealed class DatabaseSeeder
{
    public readonly FeatureFlag AccountOverviewFlag;
    public readonly FeatureFlag BetaFeaturesFlag;
    public readonly FeatureFlag CompactViewFlag;
    public readonly FeatureFlag ExperimentalUiFlag;
    public readonly FeatureFlag SsoFlag;
    public readonly Tenant Tenant1;
    public readonly User Tenant1Member;
    public readonly Session Tenant1MemberSession;
    public readonly User Tenant1Owner;
    public readonly Session Tenant1OwnerSession;
    public readonly Subscription Tenant1Subscription;

    public DatabaseSeeder(AccountDbContext accountDbContext)
    {
        Tenant1 = Tenant.Create("owner@tenant-1.com", 0);
        accountDbContext.Set<Tenant>().AddRange(Tenant1);

        Tenant1Owner = User.Create(Tenant1.Id, "owner@tenant-1.com", UserRole.Owner, true, null, 0);
        accountDbContext.Set<User>().AddRange(Tenant1Owner);

        Tenant1Member = User.Create(Tenant1.Id, "member1@tenant-1.com", UserRole.Member, true, null, 1);
        accountDbContext.Set<User>().AddRange(Tenant1Member);

        Tenant1OwnerSession = Session.Create(Tenant1.Id, Tenant1Owner.Id, LoginMethod.OneTimePassword, "TestUserAgent", IPAddress.Loopback);
        accountDbContext.Set<Session>().AddRange(Tenant1OwnerSession);

        Tenant1MemberSession = Session.Create(Tenant1.Id, Tenant1Member.Id, LoginMethod.OneTimePassword, "TestUserAgent", IPAddress.Loopback);
        accountDbContext.Set<Session>().AddRange(Tenant1MemberSession);

        Tenant1Subscription = Subscription.Create(Tenant1.Id);
        accountDbContext.Set<Subscription>().Add(Tenant1Subscription);

        var now = DateTimeOffset.UtcNow;

        BetaFeaturesFlag = FeatureFlag.Create("beta-features", FeatureFlagScope.Tenant);
        BetaFeaturesFlag.Activate(now);
        accountDbContext.Set<FeatureFlag>().Add(BetaFeaturesFlag);

        SsoFlag = FeatureFlag.Create("sso", FeatureFlagScope.Tenant);
        accountDbContext.Set<FeatureFlag>().Add(SsoFlag);

        AccountOverviewFlag = FeatureFlag.Create("account-overview", FeatureFlagScope.Tenant);
        AccountOverviewFlag.Activate(now);
        accountDbContext.Set<FeatureFlag>().Add(AccountOverviewFlag);

        CompactViewFlag = FeatureFlag.Create("compact-view", FeatureFlagScope.User);
        CompactViewFlag.Activate(now);
        accountDbContext.Set<FeatureFlag>().Add(CompactViewFlag);

        ExperimentalUiFlag = FeatureFlag.Create("experimental-ui", FeatureFlagScope.User);
        ExperimentalUiFlag.Activate(now);
        accountDbContext.Set<FeatureFlag>().Add(ExperimentalUiFlag);

        accountDbContext.SaveChanges();
    }
}
