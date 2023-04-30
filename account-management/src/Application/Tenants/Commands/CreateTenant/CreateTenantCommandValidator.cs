using System.Text.RegularExpressions;
using FluentValidation;
using PlatformPlatform.AccountManagement.Domain.Tenants;

namespace PlatformPlatform.AccountManagement.Application.Tenants.Commands.CreateTenant;

public sealed class CreateTenantCommandValidator : TenantCommandValidatorBase<CreateTenantCommand>
{
    private readonly ITenantRepository _tenantRepository;

    public CreateTenantCommandValidator(ITenantRepository tenantRepository)
    {
        _tenantRepository = tenantRepository;
        RuleFor(x => x.Subdomain)
            .Length(TenantValidationConstants.SubdomainMinLength, TenantValidationConstants.SubdomainMaxLength)
            .Matches(new Regex("^[a-z0-9]*$"))
            .WithMessage("The subdomain must contain only lowercase letters and numbers.")
            .MustAsync(ValidateSubdomain)
            .WithMessage("The subdomain must be unique.");
    }

    private async Task<bool> ValidateSubdomain(string subdomain, CancellationToken cancellationToken)
    {
        return await _tenantRepository.IsSubdomainFreeAsync(subdomain, cancellationToken);
    }
}