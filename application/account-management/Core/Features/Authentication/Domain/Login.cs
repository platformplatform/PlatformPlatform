using JetBrains.Annotations;
using PlatformPlatform.AccountManagement.Features.EmailConfirmations.Domain;
using PlatformPlatform.AccountManagement.Features.Users.Domain;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.StronglyTypedIds;

namespace PlatformPlatform.AccountManagement.Features.Authentication.Domain;

public sealed class Login : AggregateRoot<LoginId>
{
    private Login(TenantId tenantId, UserId userId, EmailConfirmationId emailConfirmationId)
        : base(LoginId.NewId())
    {
        TenantId = tenantId;
        UserId = userId;
        EmailConfirmationId = emailConfirmationId;
    }

    public TenantId TenantId { get; }

    public UserId UserId { get; private set; }

    public EmailConfirmationId EmailConfirmationId { get; private set; }

    public bool Completed { get; private set; }

    public static Login Create(User user, EmailConfirmationId emailConfirmationId)
    {
        return new Login(user.TenantId, user.Id, emailConfirmationId);
    }

    public void MarkAsCompleted()
    {
        if (Completed) throw new UnreachableException("The login process id has already been created.");

        Completed = true;
    }
}

[PublicAPI]
[IdPrefix("login")]
[JsonConverter(typeof(StronglyTypedIdJsonConverter<string, LoginId>))]
public sealed record LoginId(string Value) : StronglyTypedUlid<LoginId>(Value)
{
    public override string ToString()
    {
        return Value;
    }
}
