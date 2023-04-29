using System.Text.RegularExpressions;
using FluentValidation;
using PlatformPlatform.AccountManagement.Domain.Tenants;

namespace PlatformPlatform.AccountManagement.Application.Tenants.Commands;

public sealed class CreateTenantCommandValidator : TenantCommandValidatorBase<CreateTenantCommand>
{
    public CreateTenantCommandValidator(ITenantRepository tenantRepository)
    {
        RuleFor(x => x.Subdomain)
            .NotEmpty()
            .Length(TenantValidationConstants.SubdomainMinLength, TenantValidationConstants.SubdomainMaxLength)
            .Matches(new Regex("^[a-z0-9]*$"))
            .WithMessage("The subdomain must contain only lowercase letters and numbers.")
            .MustAsync(async (subdomain, cancellationToken) =>
                await tenantRepository.IsSubdomainFreeAsync(subdomain, cancellationToken)
            )
            .WithMessage("The subdomain must be unique.");
    }
}