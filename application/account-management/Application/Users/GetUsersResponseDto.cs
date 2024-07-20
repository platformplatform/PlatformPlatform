namespace PlatformPlatform.AccountManagement.Application.Users;

public sealed record GetUsersResponseDto(int TotalCount, int PageSize, int TotalPages, int CurrentPageOffset, UserResponseDto[] Users);
