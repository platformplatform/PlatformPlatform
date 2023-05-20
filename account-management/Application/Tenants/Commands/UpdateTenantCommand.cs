using System.Net;
using MediatR;
using PlatformPlatform.AccountManagement.Domain.Tenants;
using PlatformPlatform.Foundation.DomainModeling.Cqrs;

namespace PlatformPlatform.AccountManagement.Application.Tenants.Commands;

public sealed record UpdateTenantCommand(TenantId Id, string Name, string Email, string? Phone)
    : IRequest<CommandResult<Tenant>>;

public sealed class UpdateTenantCommandHandler : IRequestHandler<UpdateTenantCommand, CommandResult<Tenant>>
{
    private readonly ITenantRepository _tenantRepository;

    public UpdateTenantCommandHandler(ITenantRepository tenantRepository)
    {
        _tenantRepository = tenantRepository;
    }

    public async Task<CommandResult<Tenant>> Handle(UpdateTenantCommand command, CancellationToken cancellationToken)
    {
        var propertyErrors = TenantValidation.ValidateName(command.Name).Errors
            .Concat(TenantValidation.ValidateEmail(command.Email).Errors)
            .Concat(TenantValidation.ValidatePhone(command.Phone).Errors)
            .ToArray();

        if (propertyErrors.Any())
        {
            return CommandResult<Tenant>.Failure(propertyErrors, HttpStatusCode.BadRequest);
        }

        var tenant = await _tenantRepository.GetByIdAsync(command.Id, cancellationToken);
        if (tenant is null)
        {
            return CommandResult<Tenant>.Failure($"Tenant with id '{command.Id}' not found.", HttpStatusCode.NotFound);
        }

        tenant.Update(command.Name, command.Email, command.Phone);

        _tenantRepository.Update(tenant);

        return tenant;
    }
}