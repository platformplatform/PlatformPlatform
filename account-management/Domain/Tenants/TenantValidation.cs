using PlatformPlatform.Foundation.DomainModeling.Validation;

namespace PlatformPlatform.AccountManagement.Domain.Tenants;

public static class TenantValidation
{
    public const int NameMinLength = 1;
    public const int NameMaxLength = 30;

    public const int SubdomainMinLength = 3;
    public const int SubdomainMaxLength = 30;

    public static string NameLengthErrorMessage =>
        $"Name must be between {NameMinLength} and {NameMaxLength} characters.";

    public static string SubdomainRequiredErrorMessage => "Subdomain is required.";

    public static string SubdomainUniqueErrorMessage => "The subdomain must be unique.";

    public static string SubdomainLengthErrorMessage =>
        $"Subdomain must be between {SubdomainMinLength} and {SubdomainMaxLength} lowercase alphanumeric characters.";

    public static string SubdomainRuleErrorMessage => "Subdomain must be alphanumeric and lowercase.";

    public static ValidationStatus ValidateName(string input)
    {
        return ValidationUtils.IsStringValid(nameof(Tenant.Name), input, NameMinLength, NameMaxLength,
            NameLengthErrorMessage);
    }

    public static ValidationStatus ValidateSubdomain(string input)
    {
        return ValidationUtils.IsStringValid(nameof(Tenant.Subdomain), input, "^[a-z0-9]*$", SubdomainMinLength,
            SubdomainMaxLength, SubdomainLengthErrorMessage);
    }

    public static ValidationStatus ValidateEmail(string input)
    {
        return ValidationUtils.IsValidEmail(nameof(Tenant.Email), input);
    }

    public static ValidationStatus ValidatePhone(string? input)
    {
        return ValidationUtils.IsValidPhone(nameof(Tenant.Phone), input);
    }
}