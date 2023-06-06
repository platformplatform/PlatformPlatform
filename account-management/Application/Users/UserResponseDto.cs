using JetBrains.Annotations;
using PlatformPlatform.AccountManagement.Domain.Users;
using PlatformPlatform.SharedKernel.DomainCore.Entities;

namespace PlatformPlatform.AccountManagement.Application.Users;

[UsedImplicitly]
public sealed record UserResponseDto(string Id, DateTime CreatedAt, DateTime? ModifiedAt, string Email,
    UserRole UserRole) : IIdentity
{
    public object GetId()
    {
        return Id;
    }
}