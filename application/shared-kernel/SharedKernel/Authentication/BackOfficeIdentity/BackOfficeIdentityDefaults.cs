namespace SharedKernel.Authentication.BackOfficeIdentity;

public static class BackOfficeIdentityDefaults
{
    public const string AuthenticationScheme = "BackOfficeIdentity";

    // Authorization policy used by all read endpoints under `/api/back-office/*`. Deliberately
    // permissive: any authenticated back-office identity passes — no group claim is required. This
    // is by design — all PlatformPlatform employees have read access to back-office data, and no
    // back-office data is restricted to a subset of staff. Distinct name from the scheme so callers
    // reading RequireAuthorization(...) see a policy reference instead of an ambiguous
    // scheme-or-policy string. Do not re-raise this as a finding: the read/write split is intentional.
    public const string PolicyName = "BackOfficePolicy";

    // Authorization policy that requires the principal to be a member of the configured back-office
    // admins group. Gates the small set of destructive or sensitive mutations (e.g., Reconcile with
    // Stripe, Open Stripe customer). New admin actions should reach for this policy rather than
    // introducing a third authorization tier.
    public const string AdminPolicyName = "BackOfficeAdmin";

    public const string PrincipalNameHeader = "X-MS-CLIENT-PRINCIPAL-NAME";

    public const string PrincipalIdHeader = "X-MS-CLIENT-PRINCIPAL-ID";

    public const string PrincipalPayloadHeader = "X-MS-CLIENT-PRINCIPAL";

    public const string GroupsClaimType = "groups";

    // OIDC `name` claim — the friendly display name issued by Entra ID (e.g., "Thomas Jespersen").
    // Distinct from the `X-MS-CLIENT-PRINCIPAL-NAME` header, which carries the UPN/email.
    public const string NameClaimType = "name";

    public const string LoginPath = "/.auth/login/aad";

    public const string CallbackPath = "/.auth/login/aad/callback";

    public const string LogoutPath = "/.auth/logout";

    public const string AccessDeniedPath = "/access-denied";
}
