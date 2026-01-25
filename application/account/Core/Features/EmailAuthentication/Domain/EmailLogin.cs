using JetBrains.Annotations;
using PlatformPlatform.Account.Features.Users.Domain;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.StronglyTypedIds;

namespace PlatformPlatform.Account.Features.EmailAuthentication.Domain;

public sealed class EmailLogin : AggregateRoot<EmailLoginId>
{
    private EmailLogin(TenantId tenantId, UserId userId, EmailConfirmationId emailConfirmationId)
        : base(EmailLoginId.NewId())
    {
        TenantId = tenantId;
        UserId = userId;
        EmailConfirmationId = emailConfirmationId;
    }

    public TenantId TenantId { get; }

    public UserId UserId { get; private set; }

    public EmailConfirmationId EmailConfirmationId { get; private set; }

    public bool Completed { get; private set; }

    public static EmailLogin Create(User user, EmailConfirmationId emailConfirmationId)
    {
        return new EmailLogin(user.TenantId, user.Id, emailConfirmationId);
    }

    public void MarkAsCompleted()
    {
        if (Completed) throw new UnreachableException("The email login process has already been completed.");

        Completed = true;
    }
}

[PublicAPI]
[IdPrefix("emlog")]
[JsonConverter(typeof(StronglyTypedIdJsonConverter<string, EmailLoginId>))]
public sealed record EmailLoginId(string Value) : StronglyTypedUlid<EmailLoginId>(Value)
{
    public override string ToString()
    {
        return Value;
    }
}
