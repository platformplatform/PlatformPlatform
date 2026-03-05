using System.Text.RegularExpressions;
using Account.Features.Tenants.Domain;
using Account.Features.Users.Domain;
using FluentValidation;
using JetBrains.Annotations;
using SharedKernel.Authentication;
using SharedKernel.Cqrs;
using SharedKernel.ExecutionContext;
using SharedKernel.Telemetry;

namespace Account.Features.Tenants.Commands;

[PublicAPI]
public sealed record UpdateTenantContactInfoCommand(string PhoneNumber, string Street, string City, string PostalCode, string Country)
    : ICommand, IRequest<Result>
{
    public string PhoneNumber { get; } = PhoneNumber.Trim();

    public string Street { get; } = Street.Trim();

    public string City { get; } = City.Trim();

    public string PostalCode { get; } = PostalCode.Trim();

    public string Country { get; } = Country.Trim().ToUpperInvariant();
}

public sealed partial class UpdateTenantContactInfoValidator : AbstractValidator<UpdateTenantContactInfoCommand>
{
    public UpdateTenantContactInfoValidator()
    {
        RuleFor(x => x.PhoneNumber)
            .Must(phone => phone.Length <= 20 && InternationalPhoneRegex().IsMatch(phone))
            .WithMessage("Phone number must be in international format and no more than 20 characters.");

        RuleFor(x => x.Street).MaximumLength(200).WithMessage("Street must be no more than 200 characters.");

        RuleFor(x => x.City).MaximumLength(100).WithMessage("City must be no more than 100 characters.");

        RuleFor(x => x.PostalCode).MaximumLength(10).WithMessage("Postal code must be no more than 10 characters.");

        RuleFor(x => x.Country).Length(2, 2).WithMessage("Country must be exactly 2 characters.");
    }

    [GeneratedRegex(@"^\+[0-9\s\-()]+$")]
    private static partial Regex InternationalPhoneRegex();
}

public sealed class UpdateTenantContactInfoHandler(ITenantRepository tenantRepository, IExecutionContext executionContext, ITelemetryEventsCollector events)
    : IRequestHandler<UpdateTenantContactInfoCommand, Result>
{
    public async Task<Result> Handle(UpdateTenantContactInfoCommand command, CancellationToken cancellationToken)
    {
        if (executionContext.UserInfo.Role != nameof(UserRole.Owner))
        {
            return Result.Forbidden("Only owners are allowed to update tenant contact information.");
        }

        var tenant = await tenantRepository.GetCurrentTenantAsync(cancellationToken);
        if (tenant is null)
        {
            return Result.Unauthorized("Tenant has been deleted.", responseHeaders: new Dictionary<string, string>
                {
                    { AuthenticationTokenHttpKeys.UnauthorizedReasonHeaderKey, nameof(UnauthorizedReason.TenantDeleted) }
                }
            );
        }

        tenant.UpdateContactInfo(command.PhoneNumber, command.Street, command.City, command.PostalCode, command.Country);
        tenantRepository.Update(tenant);

        events.CollectEvent(new TenantContactInfoUpdated());

        return Result.Success();
    }
}
