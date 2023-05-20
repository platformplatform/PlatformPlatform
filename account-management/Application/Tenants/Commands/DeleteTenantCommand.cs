using System.Net;
using MediatR;
using PlatformPlatform.AccountManagement.Domain.Tenants;
using PlatformPlatform.Foundation.DomainModeling.Cqrs;

namespace PlatformPlatform.AccountManagement.Application.Tenants.Commands;

public sealed record DeleteTenantCommand(TenantId Id) : IRequest<CommandResult<Tenant>>;

public sealed class DeleteTenantCommandHandler : IRequestHandler<DeleteTenantCommand, CommandResult<Tenant>>
{
    private readonly ITenantRepository _tenantRepository;

    public DeleteTenantCommandHandler(ITenantRepository tenantRepository)
    {
        _tenantRepository = tenantRepository;
    }

    public async Task<CommandResult<Tenant>> Handle(DeleteTenantCommand command, CancellationToken cancellationToken)
    {
        var tenant = await _tenantRepository.GetByIdAsync(command.Id, cancellationToken);
        if (tenant is null)
        {
            return CommandResult<Tenant>.Failure($"Tenant with id '{command.Id}' not found.", HttpStatusCode.NotFound);
        }

        _tenantRepository.Remove(tenant);

        return CommandResult<Tenant>.Success(null);
    }
}