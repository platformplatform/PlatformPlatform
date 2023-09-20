using FluentValidation;
using PlatformPlatform.SharedKernel.ApplicationCore.Cqrs;

namespace PlatformPlatform.AccountManagement.Application.Tenants;

public static class DeleteTenant
{
    public sealed record Command(TenantId Id) : ICommand, IRequest<Result>;

    [UsedImplicitly]
    public sealed class Handler : IRequestHandler<Command, Result>
    {
        private readonly ITenantRepository _tenantRepository;

        public Handler(ITenantRepository tenantRepository)
        {
            _tenantRepository = tenantRepository;
        }

        public async Task<Result> Handle(Command command, CancellationToken cancellationToken)
        {
            var tenant = await _tenantRepository.GetByIdAsync(command.Id, cancellationToken);
            if (tenant is null)
            {
                return Result.NotFound($"Tenant with id '{command.Id}' not found.");
            }

            _tenantRepository.Remove(tenant);
            return Result.Success();
        }
    }

    [UsedImplicitly]
    public sealed class Validator : AbstractValidator<Command>
    {
        public Validator(IUserRepository userRepository)
        {
            RuleFor(x => x.Id)
                .MustAsync(async (tenantId, cancellationToken) =>
                    await userRepository.CountTenantUsersAsync(tenantId, cancellationToken) == 0)
                .WithMessage("All users must be deleted before the tenant can be deleted.");
        }
    }
}