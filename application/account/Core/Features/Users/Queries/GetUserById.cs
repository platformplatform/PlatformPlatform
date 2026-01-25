using JetBrains.Annotations;
using Mapster;
using PlatformPlatform.Account.Features.Users.Domain;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.Domain;

namespace PlatformPlatform.Account.Features.Users.Queries;

[PublicAPI]
public sealed record GetUserByIdQuery(UserId Id) : IRequest<Result<UserDetails>>;

public sealed class GetUserByIdHandler(IUserRepository userRepository)
    : IRequestHandler<GetUserByIdQuery, Result<UserDetails>>
{
    public async Task<Result<UserDetails>> Handle(GetUserByIdQuery query, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(query.Id, cancellationToken);

        if (user is null)
        {
            return Result<UserDetails>.NotFound($"User with ID '{query.Id}' not found.");
        }

        return user.Adapt<UserDetails>();
    }
}
