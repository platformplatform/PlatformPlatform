using PlatformPlatform.Foundation.DomainModeling.Validation;

namespace PlatformPlatform.AccountManagement.Domain.Tenants;

public static class TenantValidation
{
    public static ValidationStatus ValidateName(string input)
    {
        const string errorMessage = "Name must be between 1 and 30 characters.";
        return ValidationUtils.IsStringValid(nameof(Tenant.Name), input, 1, 30, errorMessage);
    }

    public static ValidationStatus ValidateSubdomain(string input)
    {
        const string errorMessage = "Subdomains should be 3 to 30 lowercase alphanumeric characters.";
        return ValidationUtils.IsStringValid(nameof(Tenant.Subdomain), input, "^[a-z0-9]*$", 3, 30, errorMessage);
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