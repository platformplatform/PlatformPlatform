using Mapster;
using PlatformPlatform.SharedKernel.ApplicationCore.Cqrs;
using PlatformPlatform.SharedKernel.ApplicationCore.Services;
using PlatformPlatform.SharedKernel.DomainCore.Persistence;

namespace PlatformPlatform.AccountManagement.Application.Users;

[UsedImplicitly]
public sealed record GetUsersQuery(
    string? Search = null,
    UserRole? UserRole = null,
    SortableUserProperties OrderBy = SortableUserProperties.Name,
    SortOrder SortOrder = SortOrder.Ascending,
    int? PageSize = null,
    int? PageOffset = null
)
    : IRequest<Result<GetUsersResponseDto>>;

[UsedImplicitly]
public sealed class GetUsersHandler(IUserRepository userRepository, IBlobStorage blobStorage)
    : IRequestHandler<GetUsersQuery, Result<GetUsersResponseDto>>
{
    private const string AvatarsContainer = "avatars";

    public async Task<Result<GetUsersResponseDto>> Handle(GetUsersQuery query, CancellationToken cancellationToken)
    {
        var (users, count, totalPages) = await userRepository.Search(
            query.Search,
            query.UserRole,
            query.OrderBy,
            query.SortOrder,
            query.PageSize,
            query.PageOffset,
            cancellationToken
        );

        var sharedAccessSignature = blobStorage.GetSharedAccessSignature(AvatarsContainer, TimeSpan.FromMinutes(10));
        TypeAdapterConfig<User, UserResponseDto>
            .NewConfig()
            .Map(dest => dest.AvatarUrl, src => AddSharedAccessSignature(src.Avatar.Url, sharedAccessSignature));

        var userResponseDtos = users.Adapt<UserResponseDto[]>();

        return new GetUsersResponseDto(count, totalPages, query.PageOffset ?? 0, userResponseDtos);
    }

    private string? AddSharedAccessSignature(string? avatarUrl, string? sharedAccessSignature)
    {
        if (avatarUrl?.Contains(AvatarsContainer) != true)
        {
            // Do not add SAS signature if the is not in our blob storage (e.g. https://gravatar.com/...)
            return avatarUrl;
        }

        return $"{avatarUrl}{sharedAccessSignature}";
    }
}