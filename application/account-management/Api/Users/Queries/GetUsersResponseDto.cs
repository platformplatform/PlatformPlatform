namespace PlatformPlatform.AccountManagement.Api.Users.Queries;

public sealed record GetUsersResponseDto(int TotalCount, int TotalPages, int CurrentPageOffset, UserResponseDto[] Users);
