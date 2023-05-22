using FluentValidation;
using JetBrains.Annotations;
using MediatR;
using PlatformPlatform.AccountManagement.Domain.Tenants;
using PlatformPlatform.Foundation.DomainModeling.Cqrs;
using PlatformPlatform.Foundation.DomainModeling.Validation;

namespace PlatformPlatform.AccountManagement.Application.Tenants.Commands;

/// <summary>
///     The CreateTenantCommand will create a new Tenant and add it to the repository. The command will be handled
///     by <see cref="CreateTenant.Handler" />. The Tenant will not be saved to the database until the
///     UnitOfWork is committed in the UnitOfWorkPipelineBehavior.
/// </summary>
public static class CreateTenant
{
    public sealed record Command(string Name, string Subdomain, string Email, string? Phone)
        : IRequest<CommandResult<Tenant>>;

    [UsedImplicitly]
    public sealed class Handler : IRequestHandler<Command, CommandResult<Tenant>>
    {
        private readonly ITenantRepository _tenantRepository;

        public Handler(ITenantRepository tenantRepository)
        {
            _tenantRepository = tenantRepository;
        }

        public Task<CommandResult<Tenant>> Handle(Command command, CancellationToken cancellationToken)
        {
            var tenant = Tenant.Create(command.Name, command.Subdomain, command.Email, command.Phone);

            _tenantRepository.Add(tenant);

            return Task.FromResult(CommandResult<Tenant>.Success(tenant));
        }
    }

    [UsedImplicitly]
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator(ITenantRepository tenantRepository)
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.");

            RuleFor(x => x.Name)
                .Length(TenantValidation.NameMinLength, TenantValidation.NameMaxLength)
                .WithMessage(TenantValidation.NameLengthErrorMessage)
                .When(x => !string.IsNullOrEmpty(x.Name));

            RuleFor(x => x.Email).NotEmpty().WithMessage("Email is required.");

            RuleFor(x => x.Email)
                .EmailAddress().WithMessage(ValidationUtils.EmailValidationErrorMessage)
                .MaximumLength(ValidationUtils.EmailMaxLength)
                .WithMessage(ValidationUtils.EmailValidationErrorMessage)
                .When(x => !string.IsNullOrEmpty(x.Email));

            RuleFor(x => x.Phone)
                .MaximumLength(ValidationUtils.PhoneMaxLength)
                .WithMessage(ValidationUtils.PhoneLengthErrorMessage)
                .Matches(@"^\+?(\d[\d-. ]+)?(\([\d-. ]+\))?[\d-. ]+\d$")
                .WithMessage(ValidationUtils.PhoneLengthErrorMessage)
                .When(x => !string.IsNullOrEmpty(x.Phone));

            RuleFor(x => x.Subdomain).NotEmpty().WithMessage(TenantValidation.SubdomainRequiredErrorMessage);

            RuleFor(x => x.Subdomain)
                .Length(TenantValidation.SubdomainMinLength, TenantValidation.SubdomainMaxLength)
                .WithMessage(TenantValidation.SubdomainLengthErrorMessage)
                .Matches(@"^[a-z0-9]+$").WithMessage(TenantValidation.SubdomainRuleErrorMessage)
                .MustAsync(async (subdomain, cancellation) =>
                    await tenantRepository.IsSubdomainFreeAsync(subdomain, cancellation))
                .WithMessage(TenantValidation.SubdomainUniqueErrorMessage)
                .When(x => !string.IsNullOrEmpty(x.Subdomain));
        }
    }
}