using FluentValidation;
using JetBrains.Annotations;
using Mapster;
using MediatR;
using PlatformPlatform.AccountManagement.Domain.Tenants;
using PlatformPlatform.AccountManagement.Domain.Users;
using PlatformPlatform.SharedKernel.ApplicationCore.Cqrs;

namespace PlatformPlatform.AccountManagement.Application.Users;

public static class CreateUser
{
    public sealed record Command(TenantId TenantId, string Email, UserRole UserRole)
        : ICommand, IUserValidation, IRequest<Result<UserResponseDto>>;

    [UsedImplicitly]
    public sealed class Handler : IRequestHandler<Command, Result<UserResponseDto>>
    {
        private readonly IUserRepository _userRepository;

        public Handler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<Result<UserResponseDto>> Handle(Command command, CancellationToken cancellationToken)
        {
            var user = User.Create(command.TenantId, command.Email, command.UserRole);
            await _userRepository.AddAsync(user, cancellationToken);
            return user.Adapt<UserResponseDto>();
        }
    }

    [UsedImplicitly]
    public sealed class Validator : UserValidator<Command>
    {
        public Validator(IUserRepository repository, ITenantRepository tenantRepository)
        {
            RuleFor(x => x.TenantId)
                .MustAsync(async (tenantId, cancellationToken) =>
                    await tenantRepository.ExistsAsync(tenantId, cancellationToken))
                .WithMessage(x => $"The tenant '{x.TenantId}' does not exist.")
                .When(x => !string.IsNullOrEmpty(x.Email));

            RuleFor(x => x)
                .MustAsync(async (x, cancellationToken)
                    => await repository.IsEmailFreeAsync(x.TenantId, x.Email, cancellationToken))
                .WithMessage(x => $"The email '{x.Email}' is already in use by another user on this tenant.")
                .When(x => !string.IsNullOrEmpty(x.Email));
        }
    }
}