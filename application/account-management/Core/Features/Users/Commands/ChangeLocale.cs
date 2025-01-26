using FluentValidation;
using JetBrains.Annotations;
using PlatformPlatform.AccountManagement.Features.Users.Domain;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.SinglePageApp;
using PlatformPlatform.SharedKernel.Telemetry;

namespace PlatformPlatform.AccountManagement.Features.Users.Commands;

[PublicAPI]
public sealed record ChangeLocaleCommand(string Locale) : ICommand, IRequest<Result>;

public sealed class ChangeLocaleValidator : AbstractValidator<ChangeLocaleCommand>
{
    public ChangeLocaleValidator()
    {
        RuleFor(x => x.Locale)
            .Must(lang => SinglePageAppConfiguration.SupportedLocalizations.Contains(lang))
            .WithMessage($"Language must be one of the following: {string.Join(", ", SinglePageAppConfiguration.SupportedLocalizations)}");
    }
}

public sealed class ChangeLocaleHandler(IUserRepository userRepository, ITelemetryEventsCollector events)
    : IRequestHandler<ChangeLocaleCommand, Result>
{
    public async Task<Result> Handle(ChangeLocaleCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetLoggedInUserAsync(cancellationToken);
        var fromLocale = user.Locale;
        user.ChangeLocale(command.Locale);
        userRepository.Update(user);

        events.CollectEvent(new UserLocaleChanged(fromLocale, command.Locale));

        return Result.Success();
    }
}
