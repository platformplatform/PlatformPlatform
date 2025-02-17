using FluentValidation;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Identity;
using PlatformPlatform.AccountManagement.Features.EmailConfirmations.Domain;
using PlatformPlatform.SharedKernel.Authentication;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.Integrations.Email;
using PlatformPlatform.SharedKernel.Validation;

namespace PlatformPlatform.AccountManagement.Features.EmailConfirmations.Commands;

[PublicAPI]
public sealed record StartEmailConfirmationCommand(string Email, string EmailSubject, string EmailBody, EmailConfirmationType Type)
    : ICommand, IRequest<Result<StartEmailConfirmationResponse>>;

[PublicAPI]
public sealed record StartEmailConfirmationResponse(EmailConfirmationId EmailConfirmationId, int ValidForSeconds);

public sealed class StartEmailConfirmationValidator : AbstractValidator<StartEmailConfirmationCommand>
{
    public StartEmailConfirmationValidator()
    {
        RuleFor(x => x.Email).NotEmpty().SetValidator(new SharedValidations.Email());
        RuleFor(x => x.EmailSubject).NotEmpty().MaximumLength(100);
        RuleFor(x => x.EmailBody.Contains("{oneTimePassword}")).Equal(true);
    }
}

public sealed class StartEmailConfirmationHandler(
    IEmailConfirmationRepository emailConfirmationRepository,
    IEmailClient emailClient,
    IPasswordHasher<object> passwordHasher
) : IRequestHandler<StartEmailConfirmationCommand, Result<StartEmailConfirmationResponse>>
{
    public async Task<Result<StartEmailConfirmationResponse>> Handle(StartEmailConfirmationCommand command, CancellationToken cancellationToken)
    {
        var existingConfirmations = emailConfirmationRepository.GetByEmail(command.Email).ToArray();

        if (existingConfirmations.Any(c => !c.HasExpired()))
        {
            return Result<StartEmailConfirmationResponse>.Conflict("Email confirmation for this email has already been started. Please check your spam folder.");
        }

        if (existingConfirmations.Count(r => r.CreatedAt > TimeProvider.System.GetUtcNow().AddDays(-1)) > 3)
        {
            return Result<StartEmailConfirmationResponse>.TooManyRequests("Too many attempts to confirm this email address. Please try again later.");
        }

        var oneTimePassword = OneTimePasswordHelper.GenerateOneTimePassword(6);
        var oneTimePasswordHash = passwordHasher.HashPassword(this, oneTimePassword);
        var emailConfirmation = EmailConfirmation.Create(command.Email, oneTimePasswordHash, command.Type);

        await emailConfirmationRepository.AddAsync(emailConfirmation, cancellationToken);

        var htmlContent = command.EmailBody.Replace("{oneTimePassword}", oneTimePassword);
        await emailClient.SendAsync(emailConfirmation.Email, command.EmailSubject, htmlContent, cancellationToken);

        return new StartEmailConfirmationResponse(emailConfirmation.Id, EmailConfirmation.ValidForSeconds);
    }
}
