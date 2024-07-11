using FluentValidation;
using PlatformPlatform.AccountManagement.Api.TelemetryEvents;
using PlatformPlatform.AccountManagement.Api.Tenants.Domain;
using PlatformPlatform.AccountManagement.Api.Users.Domain;
using PlatformPlatform.SharedKernel.Api.ApiResults;
using PlatformPlatform.SharedKernel.Api.Endpoints;
using PlatformPlatform.SharedKernel.Application.Cqrs;
using PlatformPlatform.SharedKernel.Application.TelemetryEvents;

namespace PlatformPlatform.AccountManagement.Api.Tenants.Commands;

public sealed record DeleteTenantCommand(TenantId Id) : ICommand, IRequest<Result>;

public sealed class DeleteTenantEndpoint : IEndpoints
{
    private const string RoutesPrefix = "/api/account-management/tenants";

    public void MapEndpoints(IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup(RoutesPrefix).WithTags("Tenants");

        group.MapDelete("/{id}", async Task<ApiResult> ([AsParameters] DeleteTenantCommand command, ISender mediator)
            => await mediator.Send(command)
        );
    }
}

public sealed class DeleteTenantValidator : AbstractValidator<DeleteTenantCommand>
{
    public DeleteTenantValidator(UserRepository userRepository)
    {
        RuleFor(x => x.Id)
            .MustAsync(async (tenantId, cancellationToken) =>
                await userRepository.CountTenantUsersAsync(tenantId, cancellationToken) == 0
            )
            .WithMessage("All users must be deleted before the tenant can be deleted.");
    }
}

public sealed class DeleteTenantHandler(TenantRepository tenantRepository, ITelemetryEventsCollector events)
    : IRequestHandler<DeleteTenantCommand, Result>
{
    public async Task<Result> Handle(DeleteTenantCommand command, CancellationToken cancellationToken)
    {
        var tenant = await tenantRepository.GetByIdAsync(command.Id, cancellationToken);
        if (tenant is null) return Result.NotFound($"Tenant with id '{command.Id}' not found.");

        tenantRepository.Remove(tenant);

        events.CollectEvent(new TenantDeleted(tenant.Id, tenant.State));

        return Result.Success();
    }
}
