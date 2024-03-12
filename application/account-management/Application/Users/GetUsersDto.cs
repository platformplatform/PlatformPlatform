namespace PlatformPlatform.AccountManagement.Application.Users;

[UsedImplicitly]
public sealed record SearchUsersDto(int TotalCount, int TotalPages, int CurrentPageOffset, UserResponseDto[] Users);