namespace PlatformPlatform.AccountManagement.Domain.Tenants;

public static class TenantValidationConstants
{
    // The Tenant name should not be too long, as it needs to be shown in the UI
    public const int NameMinLength = 1;
    public const int NameMaxLength = 30;

    // Subdomain will be used as container name in Azure Blob storage, so it should be short and min 3 characters
    public const int SubdomainMinLength = 3;
    public const int SubdomainMaxLength = 30;

    // While emails can be longer, we will limit them to 100 characters which should be enough for most cases
    public const int EmailMaxLength = 100;

    // The ITU-T Recommendation E.164 limits phone numbers to 15 digits (including country code).
    // We add 5 extra characters to allow for spaces, dashes, parentheses, etc. 
    public const int PhoneMaxLength = 20;
}