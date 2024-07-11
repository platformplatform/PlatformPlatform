using Microsoft.AspNetCore.Identity;
using PlatformPlatform.AccountManagement.Api.AccountRegistrations.Domain;
using PlatformPlatform.AccountManagement.Api.TelemetryEvents;
using PlatformPlatform.AccountManagement.Api.Tenants.Domain;
using PlatformPlatform.SharedKernel.ApiCore.ApiResults;
using PlatformPlatform.SharedKernel.ApiCore.Endpoints;
using PlatformPlatform.SharedKernel.Application.Cqrs;
using PlatformPlatform.SharedKernel.Application.TelemetryEvents;

namespace PlatformPlatform.AccountManagement.Api.AccountRegistrations.Commands;

public sealed record CompleteAccountRegistrationCommand(string OneTimePassword)
    : ICommand, IRequest<Result>
{
    [JsonIgnore]
    public AccountRegistrationId Id { get; init; } = null!;
}

public sealed class CompleteAccountRegistrationsEndpoint : IEndpoints
{
    private const string RoutesPrefix = "/api/account-management/account-registrations";

    public void MapEndpoints(IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup(RoutesPrefix).WithTags("AccountRegistrations");

        group.MapPost("{id}/complete", async Task<ApiResult> (AccountRegistrationId id, CompleteAccountRegistrationCommand command, ISender mediator)
            => await mediator.Send(command with { Id = id })
        );
    }
}

public sealed class CompleteAccountRegistrationHandler(
    TenantRepository tenantRepository,
    AccountRegistrationRepository accountRegistrationRepository,
    IPasswordHasher<object> passwordHasher,
    ITelemetryEventsCollector events,
    ILogger<CompleteAccountRegistrationHandler> logger
) : IRequestHandler<CompleteAccountRegistrationCommand, Result>
{
    public async Task<Result> Handle(CompleteAccountRegistrationCommand command, CancellationToken cancellationToken)
    {
        var accountRegistration = await accountRegistrationRepository.GetByIdAsync(command.Id, cancellationToken);

        if (accountRegistration is null)
        {
            return Result.NotFound($"AccountRegistration with id '{command.Id}' not found.");
        }

        if (passwordHasher.VerifyHashedPassword(this, accountRegistration.OneTimePasswordHash, command.OneTimePassword)
            == PasswordVerificationResult.Failed)
        {
            accountRegistration.RegisterInvalidPasswordAttempt();
            accountRegistrationRepository.Update(accountRegistration);
            events.CollectEvent(new AccountRegistrationFailed(accountRegistration.RetryCount));
            return Result.BadRequest("The code is wrong or no longer valid.", true);
        }

        if (accountRegistration.Completed)
        {
            logger.LogWarning(
                "AccountRegistration with id '{AccountRegistrationId}' has already been completed.", accountRegistration.Id
            );
            return Result.BadRequest(
                $"The account registration {accountRegistration.Id} for tenant {accountRegistration.TenantId} has already been completed."
            );
        }

        if (accountRegistration.RetryCount >= AccountRegistration.MaxAttempts)
        {
            events.CollectEvent(new AccountRegistrationBlocked(accountRegistration.RetryCount));
            return Result.Forbidden("To many attempts, please request a new code.", true);
        }

        var registrationTimeInSeconds = (TimeProvider.System.GetUtcNow() - accountRegistration.CreatedAt).TotalSeconds;
        if (accountRegistration.HasExpired())
        {
            events.CollectEvent(new AccountRegistrationExpired((int)registrationTimeInSeconds));
            return Result.BadRequest("The code is no longer valid, please request a new code.", true);
        }

        var tenant = Tenant.Create(accountRegistration.TenantId, accountRegistration.Email);
        await tenantRepository.AddAsync(tenant, cancellationToken);

        accountRegistration.MarkAsCompleted();
        accountRegistrationRepository.Update(accountRegistration);

        events.CollectEvent(new AccountRegistrationCompleted(tenant.Id, tenant.State, (int)registrationTimeInSeconds));

        return Result.Success();
    }
}
