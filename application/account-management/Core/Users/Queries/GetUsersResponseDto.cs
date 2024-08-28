namespace PlatformPlatform.AccountManagement.Core.Users.Queries;

public sealed record GetUsersResponseDto(int TotalCount, int PageSize, int TotalPages, int CurrentPageOffset, UserResponseDto[] Users);
