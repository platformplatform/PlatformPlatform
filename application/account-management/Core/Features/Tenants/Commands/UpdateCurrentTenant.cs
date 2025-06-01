using FluentValidation;
using JetBrains.Annotations;
using PlatformPlatform.AccountManagement.Features.Tenants.Domain;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.Telemetry;

namespace PlatformPlatform.AccountManagement.Features.Tenants.Commands;

[PublicAPI]
public sealed record UpdateCurrentTenantCommand : ICommand, IRequest<Result>
{
    public required string Name { get; init; }

    public string? Street { get; init; }

    public string? City { get; init; }

    public string? Zip { get; init; }

    public string? State { get; init; }

    public string? Country { get; init; }
}

public sealed class UpdateCurrentTenantValidator : AbstractValidator<UpdateCurrentTenantCommand>
{
    public UpdateCurrentTenantValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Name).Length(1, 30)
            .WithMessage("Name must be between 1 and 30 characters.")
            .When(x => !string.IsNullOrEmpty(x.Name));

        RuleFor(x => x.Street).MaximumLength(100)
            .WithMessage("Street must be 100 characters or less.")
            .When(x => !string.IsNullOrEmpty(x.Street));

        RuleFor(x => x.City).MaximumLength(50)
            .WithMessage("City must be 50 characters or less.")
            .When(x => !string.IsNullOrEmpty(x.City));

        RuleFor(x => x.Zip).MaximumLength(20)
            .WithMessage("Zip must be 20 characters or less.")
            .When(x => !string.IsNullOrEmpty(x.Zip));

        RuleFor(x => x.State).MaximumLength(50)
            .WithMessage("State must be 50 characters or less.")
            .When(x => !string.IsNullOrEmpty(x.State));

        RuleFor(x => x.Country).MaximumLength(50)
            .WithMessage("Country must be 50 characters or less.")
            .When(x => !string.IsNullOrEmpty(x.Country));
    }
}

public sealed class UpdateTenantHandler(ITenantRepository tenantRepository, ITelemetryEventsCollector events)
    : IRequestHandler<UpdateCurrentTenantCommand, Result>
{
    public async Task<Result> Handle(UpdateCurrentTenantCommand command, CancellationToken cancellationToken)
    {
        var tenant = await tenantRepository.GetCurrentTenantAsync(cancellationToken);

        var nameChanged = tenant.Name != command.Name;
        var originalAddress = tenant.Address;

        Address? newAddress = null;
        if (!string.IsNullOrEmpty(command.Street) || !string.IsNullOrEmpty(command.City) ||
            !string.IsNullOrEmpty(command.Zip) || !string.IsNullOrEmpty(command.State) ||
            !string.IsNullOrEmpty(command.Country))
        {
            newAddress = new Address(command.Street, command.City, command.Zip, command.State, command.Country);
        }

        var addressChanged = !Equals(originalAddress, newAddress);

        tenant.Update(command.Name, newAddress);
        tenantRepository.Update(tenant);

        events.CollectEvent(new TenantUpdated(nameChanged, addressChanged));

        if (addressChanged)
        {
            var hasCompleteAddress = newAddress is not null &&
                                     !string.IsNullOrEmpty(newAddress.Street) &&
                                     !string.IsNullOrEmpty(newAddress.City) &&
                                     !string.IsNullOrEmpty(newAddress.Zip) &&
                                     !string.IsNullOrEmpty(newAddress.State) &&
                                     !string.IsNullOrEmpty(newAddress.Country);

            events.CollectEvent(new TenantAddressUpdated(hasCompleteAddress));
        }

        return Result.Success();
    }
}
