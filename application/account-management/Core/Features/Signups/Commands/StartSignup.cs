using FluentValidation;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Identity;
using PlatformPlatform.AccountManagement.Features.Signups.Domain;
using PlatformPlatform.AccountManagement.Features.Tenants.Domain;
using PlatformPlatform.AccountManagement.TelemetryEvents;
using PlatformPlatform.SharedKernel.Authentication;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.Integrations.Email;
using PlatformPlatform.SharedKernel.Telemetry;
using PlatformPlatform.SharedKernel.Validation;

namespace PlatformPlatform.AccountManagement.Features.Signups.Commands;

[PublicAPI]
public sealed record StartSignupCommand(string Subdomain, string Email) : ICommand, IRequest<Result<StartSignupResponse>>
{
    public TenantId GetTenantId()
    {
        return new TenantId(Subdomain);
    }
}

[PublicAPI]
public sealed record StartSignupResponse(string SignupId, int ValidForSeconds);

public sealed class StartSignupValidator : AbstractValidator<StartSignupCommand>
{
    public StartSignupValidator(ITenantRepository tenantRepository)
    {
        RuleFor(x => x.Subdomain)
            .NotEmpty()
            .WithMessage("Subdomain must be between 3 to 30 lowercase letters, numbers, or hyphens.")
            .Matches("^(?=.{3,30}$)[a-z0-9]+(?:-[a-z0-9]+)*$")
            .WithMessage("Subdomain must be between 3 to 30 lowercase letters, numbers, or hyphens.")
            .MustAsync(tenantRepository.IsSubdomainFreeAsync)
            .WithMessage("The subdomain is not available.");
        RuleFor(x => x.Email).NotEmpty().SetValidator(new SharedValidations.Email());
    }
}

public sealed class StartSignupCommandHandler(
    ISignupRepository signupRepository,
    IEmailClient emailClient,
    IPasswordHasher<object> passwordHasher,
    ITelemetryEventsCollector events
) : IRequestHandler<StartSignupCommand, Result<StartSignupResponse>>
{
    public async Task<Result<StartSignupResponse>> Handle(StartSignupCommand command, CancellationToken cancellationToken)
    {
        var existingSignups = signupRepository.GetByEmailOrTenantId(command.GetTenantId(), command.Email);

        if (existingSignups.Any(s => !s.HasExpired()))
        {
            return Result<StartSignupResponse>.Conflict("Signup for this subdomain/mail has already been started. Please check your spam folder.");
        }

        if (existingSignups.Count(r => r.CreatedAt > TimeProvider.System.GetUtcNow().AddDays(-1)) > 3)
        {
            return Result<StartSignupResponse>.TooManyRequests("Too many attempts to signup with this email address. Please try again later.");
        }

        var oneTimePassword = OneTimePasswordHelper.GenerateOneTimePassword(6);
        var oneTimePasswordHash = passwordHasher.HashPassword(this, oneTimePassword);
        var signup = Signup.Create(command.GetTenantId(), command.Email, oneTimePasswordHash);

        await signupRepository.AddAsync(signup, cancellationToken);
        events.CollectEvent(new SignupStarted(command.GetTenantId()));

        await emailClient.SendAsync(signup.Email, "Confirm your email address",
            $"""
             <h1 style="text-align:center;font-family=sans-serif;font-size:20px">Your confirmation code is below</h1>
             <p style="text-align:center;font-family=sans-serif;font-size:16px">Enter it in your open browser window. It is only valid for a few minutes.</p>
             <p style="text-align:center;font-family=sans-serif;font-size:40px;background:#f5f4f5">{oneTimePassword}</p>
             """,
            cancellationToken
        );

        return new StartSignupResponse(signup.Id, Signup.ValidForSeconds);
    }
}
