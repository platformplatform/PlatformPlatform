using Account.Features.Users.Domain;
using Account.Features.Users.Shared;
using FluentValidation;
using JetBrains.Annotations;
using SharedKernel.Cqrs;
using SharedKernel.SinglePageApp;
using SharedKernel.Telemetry;

namespace Account.Features.Users.Commands;

[PublicAPI]
public sealed record ChangeLocaleCommand(string Locale) : ICommand, IRequest<Result<UserResponse>>;

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
    : IRequestHandler<ChangeLocaleCommand, Result<UserResponse>>
{
    public async Task<Result<UserResponse>> Handle(ChangeLocaleCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetLoggedInUserAsync(cancellationToken);
        var fromLocale = user.Locale;
        user.ChangeLocale(command.Locale);
        userRepository.Update(user);

        events.CollectEvent(new UserLocaleChanged(fromLocale, command.Locale));

        return UserResponse.FromUser(user);
    }
}
