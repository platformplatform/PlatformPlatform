namespace PlatformPlatform.AccountManagement.Application.Users;

public sealed record GetUsersResponseDto(int TotalCount, int TotalPages, int CurrentPageOffset, UserResponseDto[] Users);
