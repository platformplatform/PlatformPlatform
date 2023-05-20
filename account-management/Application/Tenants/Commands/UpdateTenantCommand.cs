using System.Net;
using MediatR;
using PlatformPlatform.AccountManagement.Domain.Tenants;
using PlatformPlatform.Foundation.DomainModeling.Cqrs;
using PlatformPlatform.Foundation.DomainModeling.Validation;

namespace PlatformPlatform.AccountManagement.Application.Tenants.Commands;

public sealed record UpdateTenantCommand(TenantId TenantId, string Name, string Email, string? Phone)
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
        var tenant = await _tenantRepository.GetByIdAsync(command.TenantId, cancellationToken);

        if (tenant is null)
        {
            return CommandResult<Tenant>.Failure(new[] {new PropertyError("TenantId", "Tenant not found.")},
                HttpStatusCode.NotFound);
        }

        var propertyErrors = TenantValidation.ValidateName(command.Name).Errors
            .Concat(TenantValidation.ValidateEmail(command.Email).Errors)
            .Concat(TenantValidation.ValidatePhone(command.Phone).Errors)
            .ToArray();

        if (propertyErrors.Any())
        {
            return CommandResult<Tenant>.Failure(propertyErrors, HttpStatusCode.BadRequest);
        }

        tenant.Update(command.Name, command.Email, command.Phone);

        _tenantRepository.Update(tenant);

        return tenant;
    }
}