using MediatR;
using PlatformPlatform.AccountManagement.Domain.Users;
using PlatformPlatform.SharedKernel.ApplicationCore.Cqrs;

namespace PlatformPlatform.AccountManagement.Application.Users.Queries;

public static class GetUser
{
    public sealed record Query(UserId Id) : IRequest<Result<User>>;

    public sealed class Handler : IRequestHandler<Query, Result<User>>
    {
        private readonly IUserRepository _userRepository;

        public Handler(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<Result<User>> Handle(Query request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByIdAsync(request.Id, cancellationToken);
            return user ?? Result<User>.NotFound($"User with id '{request.Id}' not found.");
        }
    }
}