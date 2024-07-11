using PlatformPlatform.AccountManagement.Api.TelemetryEvents;
using PlatformPlatform.AccountManagement.Api.Users.Domain;
using PlatformPlatform.SharedKernel.Api.ApiResults;
using PlatformPlatform.SharedKernel.Api.Endpoints;
using PlatformPlatform.SharedKernel.Application.Cqrs;
using PlatformPlatform.SharedKernel.Application.TelemetryEvents;

namespace PlatformPlatform.AccountManagement.Api.Users.Commands;

public sealed record DeleteUserCommand(UserId Id) : ICommand, IRequest<Result>;

public sealed class DeleteUserEndpoint : IEndpoints
{
    private const string RoutesPrefix = "/api/account-management/users";

    public void MapEndpoints(IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup(RoutesPrefix).WithTags("Users");

        group.MapDelete("/{id}", async Task<ApiResult> ([AsParameters] DeleteUserCommand command, ISender mediator)
            => await mediator.Send(command)
        );
    }
}

public sealed class DeleteUserHandler(UserRepository userRepository, ITelemetryEventsCollector events)
    : IRequestHandler<DeleteUserCommand, Result>
{
    public async Task<Result> Handle(DeleteUserCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(command.Id, cancellationToken);
        if (user is null) return Result.NotFound($"User with id '{command.Id}' not found.");

        userRepository.Remove(user);

        events.CollectEvent(new UserDeleted());

        return Result.Success();
    }
}
