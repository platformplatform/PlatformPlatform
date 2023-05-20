using System.Net;
using MediatR;
using PlatformPlatform.AccountManagement.Application.Tenants.Dtos;
using PlatformPlatform.AccountManagement.Domain.Tenants;
using PlatformPlatform.Foundation.DomainModeling.Cqrs;
using PlatformPlatform.Foundation.DomainModeling.Validation;

namespace PlatformPlatform.AccountManagement.Application.Tenants.Commands;

public sealed record DeleteTenantCommand(TenantId Id) : IRequest<CommandResult<TenantDto>>;

public sealed class DeleteTenantCommandHandler : IRequestHandler<DeleteTenantCommand, CommandResult<TenantDto>>
{
    private readonly ITenantRepository _tenantRepository;

    public DeleteTenantCommandHandler(ITenantRepository tenantRepository)
    {
        _tenantRepository = tenantRepository;
    }

    public async Task<CommandResult<TenantDto>> Handle(DeleteTenantCommand command, CancellationToken cancellationToken)
    {
        var tenant = await _tenantRepository.GetByIdAsync(command.Id, cancellationToken);
        if (tenant is null)
        {
            return CommandResult<TenantDto>.Failure(new[] {new PropertyError("TenantId", "Tenant not found.")},
                HttpStatusCode.NotFound);
        }

        _tenantRepository.Remove(tenant);

        return CommandResult<TenantDto>.Success(null);
    }
}