using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using PlatformPlatform.AccountManagement.Database;
using PlatformPlatform.AccountManagement.Signups.Commands;
using PlatformPlatform.AccountManagement.Tenants.EventHandlers;
using Xunit;

namespace PlatformPlatform.AccountManagement.Tests.Signups;

public sealed class SignupCommandsTests : BaseTest<AccountManagementDbContext>
{
    [Fact]
    public async Task StartSignup_WhenValidCommand_ShouldReturnSuccessfulResult()
    {
        // Arrange
        var subdomain = Faker.Subdomain();
        var email = Faker.Internet.Email();
        var command = new StartSignupCommand(subdomain, email);
        var mediator = Provider.GetRequiredService<ISender>();

        // Act
        var result = await mediator.Send(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Errors.Should().BeNull();

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents.Count(e =>
            e.Name == "SignupStarted" &&
            e.Properties["event_TenantId"] == subdomain
        ).Should().Be(1);

        await EmailService.Received().SendAsync(email.ToLower(), "Confirm your email address", Arg.Any<string>(), CancellationToken.None);
    }

    [Fact]
    public async Task StartSignup_WhenInvalidEmail_ShouldFail()
    {
        // Arrange
        var subdomain = Faker.Subdomain();
        var command = new StartSignupCommand(subdomain, "invalid email");
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
    [InlineData("Subdomain special characters", "tenant%2")]
    [InlineData("Subdomain with spaces", "tenant 2")]
    public async Task StartSignup_WhenInvalidSubDomain_ShouldFail(string scenario, string subdomain)
    {
        // Arrange
        var email = Faker.Internet.Email();
        var command = new StartSignupCommand(subdomain, email);
        var mediator = Provider.GetRequiredService<ISender>();

        // Act
        var result = await mediator.Send(command);

        // Assert
        result.IsSuccess.Should().BeFalse(scenario);
        result.Errors?.Length.Should().Be(1, scenario);
    }

    [Fact]
    public async Task CompleteSignupTests_WhenSucceeds_ShouldLogCorrectInformation()
    {
        // Arrange
        var mockLogger = Substitute.For<ILogger<TenantCreatedEventHandler>>();
        var mockHttpContextAccessor = Substitute.For<IHttpContextAccessor>();
        var mockHttpContext = new DefaultHttpContext();

        mockHttpContextAccessor.HttpContext.Returns(mockHttpContext);

        Services.AddSingleton(mockLogger);
        Services.AddSingleton(mockHttpContextAccessor);

        var mediator = Provider.GetRequiredService<ISender>();

        // Act
        var command = new CompleteSignupCommand(DatabaseSeeder.OneTimePassword);
        _ = await mediator.Send(command with { Id = DatabaseSeeder.Signup1.Id });

        // Assert
        mockLogger.Received().LogInformation("Raise event to send Welcome mail to tenant.");
    }
}
