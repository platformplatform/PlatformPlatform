using Mapster;
using MediatR;
using PlatformPlatform.AccountManagement.Application.Tenants.Dtos;
using PlatformPlatform.AccountManagement.Domain.Tenants;
using PlatformPlatform.Foundation.Application;
using PlatformPlatform.Foundation.Domain;

namespace PlatformPlatform.AccountManagement.Application.Tenants.Commands.CreateTenant;

/// <summary>
///     The CreateTenantCommand will create a new Tenant and add it to the repository. The command will be handled
///     by <see cref="CreateTenantCommandHandler" />. The Tenant will not be saved to the database until the
///     UnitOfWork is committed in the UnitOfWorkPipelineBehavior.
/// </summary>
public sealed record CreateTenantCommand(string Name, string Subdomain, string Email, string? Phone) :
    IRequest<CommandResult<TenantDto>>;

public sealed class CreateTenantCommandHandler : IRequestHandler<CreateTenantCommand, CommandResult<TenantDto>>
{
    private readonly ITenantRepository _tenantRepository;

    public CreateTenantCommandHandler(ITenantRepository tenantRepository)
    {
        _tenantRepository = tenantRepository;
    }

    public async Task<CommandResult<TenantDto>> Handle(CreateTenantCommand command, CancellationToken cancellationToken)
    {
        var isUniqueSubdomain = await IsSubdomainUniqueAsync(command.Subdomain, cancellationToken);

        var propertyErrors = TenantValidation.ValidateName(command.Name).Errors
            .Concat(TenantValidation.ValidateEmail(command.Email).Errors)
            .Concat(TenantValidation.ValidatePhone(command.Phone).Errors)
            .Concat(TenantValidation.ValidateSubdomain(command.Subdomain).Errors)
            .Concat(isUniqueSubdomain.Errors)
            .ToArray();

        if (propertyErrors.Any())
        {
            return CommandResult<TenantDto>.Failure(propertyErrors);
        }

        var tenant = Tenant.Create(command.Name, command.Subdomain, command.Email, command.Phone);

        _tenantRepository.Add(tenant);

        return tenant.Adapt<TenantDto>();
    }

    private async Task<Result> IsSubdomainUniqueAsync(string subdomain, CancellationToken cancellationToken)
    {
        var isSubdomainUnique = await _tenantRepository.IsSubdomainFreeAsync(subdomain, cancellationToken);
        return isSubdomainUnique
            ? Result.Success()
            : Result.Failure(nameof(Tenant.Subdomain), "The subdomain must be unique.");
    }
}