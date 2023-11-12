using FluentValidation;
using PlatformPlatform.SharedKernel.ApplicationCore.Cqrs;

namespace PlatformPlatform.AccountManagement.Application.Tenants;

public sealed record DeleteTenantCommand(TenantId Id) : ICommand, IRequest<Result>;

[UsedImplicitly]
public sealed class DeleteTenantHandler : IRequestHandler<DeleteTenantCommand, Result>
{
    private readonly ITenantRepository _tenantRepository;

    public DeleteTenantHandler(ITenantRepository tenantRepository)
    {
        _tenantRepository = tenantRepository;
    }

    public async Task<Result> Handle(DeleteTenantCommand command, CancellationToken cancellationToken)
    {
        var tenant = await _tenantRepository.GetByIdAsync(command.Id, cancellationToken);
        if (tenant is null) return Result.NotFound($"Tenant with id '{command.Id}' not found.");

        _tenantRepository.Remove(tenant);
        return Result.Success();
    }
}

[UsedImplicitly]
public sealed class DeleteTenantValidator : AbstractValidator<DeleteTenantCommand>
{
    public DeleteTenantValidator(IUserRepository userRepository)
    {
        RuleFor(x => x.Id)
            .MustAsync(async (tenantId, cancellationToken) =>
                await userRepository.CountTenantUsersAsync(tenantId, cancellationToken) == 0)
            .WithMessage("All users must be deleted before the tenant can be deleted.");
    }
}