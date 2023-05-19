using System.Net;
using Mapster;
using MediatR;
using PlatformPlatform.AccountManagement.Application.Tenants.Dtos;
using PlatformPlatform.AccountManagement.Domain.Tenants;
using PlatformPlatform.Foundation.DddCore.Cqrs;
using PlatformPlatform.Foundation.DddCore.Validation;

namespace PlatformPlatform.AccountManagement.Application.Tenants.Commands.UpdateTenant;

public sealed record UpdateTenantCommand(TenantId TenantId, string Name, string Email, string? Phone)
    : IRequest<CommandResult<TenantDto>>;

public sealed class UpdateTenantCommandHandler : IRequestHandler<UpdateTenantCommand, CommandResult<TenantDto>>
{
    private readonly ITenantRepository _tenantRepository;

    public UpdateTenantCommandHandler(ITenantRepository tenantRepository)
    {
        _tenantRepository = tenantRepository;
    }

    public async Task<CommandResult<TenantDto>> Handle(UpdateTenantCommand command, CancellationToken cancellationToken)
    {
        var tenant = await _tenantRepository.GetByIdAsync(command.TenantId, cancellationToken);

        if (tenant is null)
        {
            return CommandResult<TenantDto>.Failure(new[] {new PropertyError("TenantId", "Tenant not found.")},
                HttpStatusCode.NotFound);
        }

        var propertyErrors = TenantValidation.ValidateName(command.Name).Errors
            .Concat(TenantValidation.ValidateEmail(command.Email).Errors)
            .Concat(TenantValidation.ValidatePhone(command.Phone).Errors)
            .ToArray();

        if (propertyErrors.Any())
        {
            return CommandResult<TenantDto>.Failure(propertyErrors, HttpStatusCode.BadRequest);
        }

        tenant.Update(command.Name, command.Email, command.Phone);

        _tenantRepository.Update(tenant);

        return tenant.Adapt<TenantDto>();
    }
}