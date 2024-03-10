using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using PlatformPlatform.AccountManagement.Application.AccountRegistrations;
using PlatformPlatform.AccountManagement.Application.Tenants;
using PlatformPlatform.AccountManagement.Infrastructure;
using Xunit;

namespace PlatformPlatform.AccountManagement.Tests.Application.AccountRegistrations;

public sealed class AccountRegistrationTests : BaseTest<AccountManagementDbContext>
{
    [Fact]
    public async Task StartAccountRegistration_WhenValidCommand_ShouldReturnSuccessfulResult()
    {
        // Arrange
        var subdomain = Faker.Subdomain();
        var email = Faker.Internet.Email();
        var command = new StartAccountRegistrationCommand(subdomain, email);
        var mediator = Provider.GetRequiredService<ISender>();

        // Act
        var result = await mediator.Send(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Errors.Should().BeNull();

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents.Count(e =>
            e.Name == "AccountRegistrationStarted" &&
            e.Properties["Event_TenantId"] == subdomain
        ).Should().Be(1);

        await EmailService.Received().SendAsync(email.ToLower(), "Confirm your email address", Arg.Any<string>(),
            CancellationToken.None);
    }

    [Fact]
    public async Task StartAccountRegistration_WhenInvalidEmail_ShouldFail()
    {
        // Arrange
        var subdomain = Faker.Subdomain();
        var command = new StartAccountRegistrationCommand(subdomain, "invalid email");
        var mediator = Provider.GetRequiredService<ISender>();

        // Act
        var result = await mediator.Send(command);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors?.Length.Should().Be(1);
    }

    [Theory]
    [InlineData("Subdomain empty", "")]
    [InlineData("Subdomain too short", "ab")]
    [InlineData("Subdomain too long", "1234567890123456789012345678901")]
    [InlineData("Subdomain with uppercase", "Tenant2")]
    [InlineData("Subdomain special characters", "tenant-2")]
    [InlineData("Subdomain with spaces", "tenant 2")]
    public async Task StartAccountRegistration_WhenInvalidSubDomain_ShouldFail(string scenario, string subdomain)
    {
        // Arrange
        var email = Faker.Internet.Email();
        var command = new StartAccountRegistrationCommand(subdomain, email);
        var mediator = Provider.GetRequiredService<ISender>();

        // Act
        var result = await mediator.Send(command);

        // Assert
        result.IsSuccess.Should().BeFalse(scenario);
        result.Errors?.Length.Should().Be(1, scenario);
    }

    [Fact]
    public async Task CompleteAccountRegistrationTests_WhenSucceded_ShouldLogCorrectInformation()
    {
        // Arrange
        var mockLogger = Substitute.For<ILogger<TenantCreatedEventHandler>>();
        Services.AddSingleton(mockLogger);
        var mediator = Provider.GetRequiredService<ISender>();

        // Act
        var command = new CompleteAccountRegistrationCommand(DatabaseSeeder.AccountRegistration1.OneTimePassword);
        _ = await mediator.Send(command with { Id = DatabaseSeeder.AccountRegistration1.Id });

        // Assert
        mockLogger.Received().LogInformation("Raise event to send Welcome mail to tenant.");
    }
}