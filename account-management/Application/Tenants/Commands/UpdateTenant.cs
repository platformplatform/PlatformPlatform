using System.Net;
using MediatR;
using PlatformPlatform.AccountManagement.Domain.Tenants;
using PlatformPlatform.Foundation.DomainModeling.Cqrs;

namespace PlatformPlatform.AccountManagement.Application.Tenants.Commands;

public static class UpdateTenant
{
    public sealed record Command(TenantId Id, string Name, string Email, string? Phone)
        : IRequest<CommandResult<Tenant>>;

    public sealed class Handler : IRequestHandler<Command, CommandResult<Tenant>>
    {
        private readonly ITenantRepository _tenantRepository;

        public Handler(ITenantRepository tenantRepository)
        {
            _tenantRepository = tenantRepository;
        }

        public async Task<CommandResult<Tenant>> Handle(Command command, CancellationToken cancellationToken)
        {
            var attributeErrors = TenantValidation.ValidateName(command.Name).Errors
                .Concat(TenantValidation.ValidateEmail(command.Email).Errors)
                .Concat(TenantValidation.ValidatePhone(command.Phone).Errors)
                .ToArray();

            if (attributeErrors.Any())
            {
                return CommandResult<Tenant>.AttributesFailure(attributeErrors, HttpStatusCode.BadRequest);
            }

            var tenant = await _tenantRepository.GetByIdAsync(command.Id, cancellationToken);
            if (tenant is null)
            {
                return CommandResult<Tenant>.GenericFailure($"Tenant with id '{command.Id}' not found.",
                    HttpStatusCode.NotFound);
            }

            tenant.Update(command.Name, command.Email, command.Phone);

            _tenantRepository.Update(tenant);

            return tenant;
        }
    }
}