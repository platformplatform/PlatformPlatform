using PlatformPlatform.Foundation.DddCore.Validation;

namespace PlatformPlatform.AccountManagement.Domain.Tenants;

public static class TenantValidation
{
    public static ValidationResult ValidateName(string input)
    {
        const string errorMessage = "Name must be between 1 and 30 characters.";
        return ValidationUtils.IsStringValid(nameof(Tenant.Name), input, 1, 30, errorMessage);
    }

    public static ValidationResult ValidateSubdomain(string input)
    {
        const string errorMessage = "Subdomains should be 3 to 30 lowercase alphanumeric characters.";
        return ValidationUtils.IsStringValid(nameof(Tenant.Subdomain), input, "^[a-z0-9]*$", 3, 30, errorMessage);
    }

    public static ValidationResult ValidateEmail(string input)
    {
        return ValidationUtils.IsValidEmail(nameof(Tenant.Email), input);
    }

    public static ValidationResult ValidatePhone(string? input)
    {
        return ValidationUtils.IsValidPhone(nameof(Tenant.Phone), input);
    }
}