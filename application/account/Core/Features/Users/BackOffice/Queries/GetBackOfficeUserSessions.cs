using Account.Features.Authentication.Domain;
using Account.Features.Users.Domain;
using FluentValidation;
using JetBrains.Annotations;
using SharedKernel.Authentication.TokenGeneration;
using SharedKernel.Cqrs;
using SharedKernel.Domain;

namespace Account.Features.Users.BackOffice.Queries;

[PublicAPI]
public sealed record GetBackOfficeUserSessionsQuery(int PageOffset = 0, int PageSize = 25) : IRequest<Result<BackOfficeUserSessionsResponse>>
{
    [JsonIgnore] // Removes from API contract
    public UserId Id { get; init; } = null!;
}

[PublicAPI]
public sealed record BackOfficeUserSessionsResponse(int TotalCount, int PageSize, int TotalPages, int CurrentPageOffset, BackOfficeUserSession[] Sessions);

[PublicAPI]
public sealed record BackOfficeUserSession(
    SessionId Id,
    LoginMethod LoginMethod,
    DeviceType DeviceType,
    string UserAgent,
    string IpAddress,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastActiveAt,
    DateTimeOffset? RevokedAt,
    SessionRevokedReason? RevokedReason,
    DateTimeOffset ExpiresAt
);

public sealed class GetBackOfficeUserSessionsQueryValidator : AbstractValidator<GetBackOfficeUserSessionsQuery>
{
    public GetBackOfficeUserSessionsQueryValidator()
    {
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100).WithMessage("Page size must be between 1 and 100.");
        RuleFor(x => x.PageOffset).GreaterThanOrEqualTo(0).WithMessage("Page offset must be greater than or equal to 0.");
    }
}

public sealed class GetBackOfficeUserSessionsHandler(IUserRepository userRepository, ISessionRepository sessionRepository)
    : IRequestHandler<GetBackOfficeUserSessionsQuery, Result<BackOfficeUserSessionsResponse>>
{
    public async Task<Result<BackOfficeUserSessionsResponse>> Handle(GetBackOfficeUserSessionsQuery query, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdUnfilteredAsync(query.Id, cancellationToken);
        if (user is null)
        {
            return Result<BackOfficeUserSessionsResponse>.NotFound($"User with id '{query.Id}' was not found.");
        }

        var (sessions, totalCount, totalPages) = await sessionRepository.GetSessionsForUserUnfilteredAsync(
            user.Id,
            query.PageOffset,
            query.PageSize,
            cancellationToken
        );

        if (query.PageOffset > 0 && query.PageOffset >= totalPages)
        {
            return Result<BackOfficeUserSessionsResponse>.BadRequest($"The page offset '{query.PageOffset}' is greater than the total number of pages.");
        }

        var summaries = sessions.Select(s => new BackOfficeUserSession(
                s.Id,
                s.LoginMethod,
                s.DeviceType,
                s.UserAgent,
                s.IpAddress,
                s.CreatedAt,
                s.ModifiedAt,
                s.RevokedAt,
                s.RevokedReason,
                s.ExpiresAt
            )
        ).ToArray();

        return new BackOfficeUserSessionsResponse(totalCount, query.PageSize, totalPages, query.PageOffset, summaries);
    }
}
