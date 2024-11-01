using System.Net;
using System.Security.Cryptography;
using System.Text;
using FluentValidation;
using JetBrains.Annotations;
using PlatformPlatform.AccountManagement.TelemetryEvents;
using PlatformPlatform.AccountManagement.Tenants.Domain;
using PlatformPlatform.AccountManagement.Users.Domain;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.ExecutionContext;
using PlatformPlatform.SharedKernel.TelemetryEvents;
using PlatformPlatform.SharedKernel.Validation;

namespace PlatformPlatform.AccountManagement.Users.Commands;

[PublicAPI]
public sealed record CreateUserCommand(TenantId TenantId, string Email, UserRole UserRole, bool EmailConfirmed)
    : ICommand, IRequest<Result<UserId>>;

public sealed class CreateUserValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserValidator(IUserRepository userRepository, ITenantRepository tenantRepository)
    {
        RuleFor(x => x.Email).NotEmpty().SetValidator(new SharedValidations.Email());

        RuleFor(x => x.TenantId)
            .MustAsync((x, cancellationToken) => tenantRepository.ExistsAsync(new TenantId(x), cancellationToken))
            .WithMessage(x => $"The tenant '{x.TenantId}' does not exist.")
            .When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x)
            .MustAsync((x, cancellationToken) => userRepository.IsEmailFreeAsync(x.Email, cancellationToken))
            .WithName("Email")
            .WithMessage(x => $"The email '{x.Email}' is already in use by another user on this tenant.")
            .When(x => !string.IsNullOrEmpty(x.Email));
    }
}

public sealed class CreateUserHandler(
    IUserRepository userRepository,
    ITelemetryEventsCollector events,
    IHttpClientFactory httpClientFactory,
    IExecutionContext executionContext,
    ILogger<CreateUserHandler> logger
) : IRequestHandler<CreateUserCommand, Result<UserId>>
{
    public async Task<Result<UserId>> Handle(CreateUserCommand command, CancellationToken cancellationToken)
    {
        if (executionContext.TenantId is not null && executionContext.TenantId != command.TenantId)
        {
            throw new UnreachableException("Only when signing up a new tenant, is the TenantID allowed to different than the current tenant.");
        }

        var gravatarUrl = await GetGravatarProfileUrlIfExists(command.Email);

        var user = User.Create(command.TenantId, command.Email, command.UserRole, command.EmailConfirmed, gravatarUrl);

        await userRepository.AddAsync(user, cancellationToken);

        events.CollectEvent(new UserCreated(command.TenantId, gravatarUrl is not null));

        return user.Id;
    }

    private async Task<string?> GetGravatarProfileUrlIfExists(string email)
    {
        try
        {
            var gravatarHttpClient = httpClientFactory.CreateClient("Gravatar");
            gravatarHttpClient.Timeout = TimeSpan.FromSeconds(5);

            var hash = Convert.ToHexString(MD5.HashData(Encoding.ASCII.GetBytes(email)));
            var gravatarUrl = $"https://gravatar.com/avatar/{hash.ToLowerInvariant()}";
            // The d=404 instructs Gravatar to return 404 if the email has no Gravatar account
            var httpResponseMessage = await gravatarHttpClient.GetAsync($"{gravatarUrl}?d=404");

            switch (httpResponseMessage.StatusCode)
            {
                case HttpStatusCode.OK:
                    return gravatarUrl;
                case HttpStatusCode.NotFound:
                    return null;
                default:
                    logger.LogError("Failed to fetch gravatar profile. Status code: {StatusCode}", httpResponseMessage.StatusCode);
                    return null;
            }
        }
        catch(TaskCanceledException e)
        {
            logger.LogError(e, "Time when fetching gravatar profile.");
            return null;
        }
    }
}
