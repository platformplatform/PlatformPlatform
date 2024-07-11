using Mapster;
using PlatformPlatform.AccountManagement.Api.Users.Domain;
using PlatformPlatform.SharedKernel.ApiCore.ApiResults;
using PlatformPlatform.SharedKernel.ApiCore.Endpoints;
using PlatformPlatform.SharedKernel.Application.Cqrs;

namespace PlatformPlatform.AccountManagement.Api.Users.Queries;

public sealed record GetUserQuery(UserId Id) : IRequest<Result<UserResponseDto>>;

public sealed record UserResponseDto(
    string Id,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ModifiedAt,
    string Email,
    UserRole Role,
    string FirstName,
    string LastName,
    string Title,
    bool EmailConfirmed,
    string? AvatarUrl
);

public sealed class GetUserEndpoint : IEndpoints
{
    private const string RoutesPrefix = "/api/account-management/users";

    public void MapEndpoints(IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup(RoutesPrefix).WithTags("Users");

        group.MapGet("/{id}", async Task<ApiResult<UserResponseDto>> ([AsParameters] GetUserQuery query, ISender mediator)
            => await mediator.Send(query)
        ).Produces<UserResponseDto>();
    }
}

public sealed class GetUserHandler(UserRepository userRepository)
    : IRequestHandler<GetUserQuery, Result<UserResponseDto>>
{
    public async Task<Result<UserResponseDto>> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.Id, cancellationToken);
        return user?.Adapt<UserResponseDto>() ?? Result<UserResponseDto>.NotFound($"User with id '{request.Id}' not found.");
    }
}
