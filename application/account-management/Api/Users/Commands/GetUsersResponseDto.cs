namespace PlatformPlatform.AccountManagement.Api.Users.Commands;

public sealed record GetUsersResponseDto(int TotalCount, int TotalPages, int CurrentPageOffset, UserResponseDto[] Users);
