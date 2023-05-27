using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using PlatformPlatform.AccountManagement.Application;
using PlatformPlatform.AccountManagement.Application.Tenants.Commands;
using PlatformPlatform.AccountManagement.Domain.Tenants;
using PlatformPlatform.Foundation.DomainModeling.Persistence;
using Xunit;

namespace PlatformPlatform.AccountManagement.Tests.Application.Tenants.Commands;

public class CreateTenantTests : IDisposable
{
    private readonly IMediator _mediator;
    private readonly ServiceProvider _provider;
    private readonly ITenantRepository _tenantRepository;

    public CreateTenantTests()
    {
        _tenantRepository = Substitute.For<ITenantRepository>();
        _tenantRepository.IsSubdomainFreeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var unitOfWork = Substitute.For<IUnitOfWork>(); // Mock IUnitOfWork

        var services = new ServiceCollection();
        services.AddApplicationServices();
        services.AddSingleton(_tenantRepository);
        services.AddSingleton(unitOfWork); // Add your mocked unitOfWork

        _provider = services.BuildServiceProvider();
        _mediator = _provider.GetRequiredService<IMediator>();
    }

    public void Dispose()
    {
        _provider.Dispose();
    }

    [Fact]
    public async Task CreateTenantHandler_WhenCommandIsValid_ShouldAddTenantToRepository()
    {
        // Arrange
        var startId = TenantId.NewId(); // NewId will always generate an id that are greater than the previous one

        // Act
        var command = new CreateTenant.Command("TestTenant", "tenant1", "foo@tenant1.com", "1234567890");
        var result = await _mediator.Send(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var tenantResponse = result.Value!;
        _tenantRepository.Received()
            .AddAsync(Arg.Is<Tenant>(t => t.Name == command.Name && t.Id > startId && t.Id == tenantResponse.Id));
    }

    [Fact]
    public async Task CreateTenantHandler_WhenCommandIsValid_ShouldReturnTenantDtoWithCorrectValues()
    {
        // Arrange

        // Act
        var command = new CreateTenant.Command("TestTenant", "tenant1", "foo@tenant1.com", "1234567890");
        var result = await _mediator.Send(command);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var tenantResponseDto = result.Value!;
        tenantResponseDto.Name.Should().Be(command.Name);
        tenantResponseDto.Email.Should().Be(command.Email);
        tenantResponseDto.Phone.Should().Be(command.Phone);
    }

    [Fact]
    public async Task CreateTenantHandler_WhenCommandIsValid_ShouldRaiseTenantCreatedEvent()
    {
        // Arrange

        // Act
        var command = new CreateTenant.Command("TestTenant", "tenant1", "foo@tenant1.com", "1234567890");
        var _ = await _mediator.Send(command);

        // Assert
        _tenantRepository.Received().AddAsync(Arg.Is<Tenant>(t => t.DomainEvents.Single() is TenantCreatedEvent));
    }

    [Theory]
    [InlineData("Valid properties", "tenant1", "foo@tenant1.com", "+44 (0)20 7946 0123", true)]
    [InlineData("No phone number (valid)", "tenant1", "foo@tenant1.com", null, true)]
    [InlineData("Empty phone number", "tenant1", "foo@tenant1.com", "", true)]
    [InlineData("To long phone number", "tenant1", "foo@tenant1.com", "0099 (999) 888-77-66-55", false)]
    [InlineData("Invalid phone number", "tenant1", "foo@tenant1.com", "N/A", false)]
    [InlineData("", "notenantname", "foo@tenant1.com", "1234567890", false)]
    [InlineData("Too long tenant name above 30 characters", "tenant1", "foo@tenant1.com", "+55 (21) 99999-9999", false)]
    [InlineData("No email", "tenant1", "", "+61 2 1234 5678", false)]
    [InlineData("Invalid Email", "tenant1", "@tenant1.com", "1234567890", false)]
    [InlineData("No subdomain", "", "foo@tenant1.com", "1234567890", false)]
    [InlineData("To short subdomain", "ab", "foo@tenant1.com", "1234567890", false)]
    [InlineData("To long subdomain", "1234567890123456789012345678901", "foo@tenant1.com", "1234567890", false)]
    [InlineData("Subdomain with uppercase", "Tenant1", "foo@tenant1.com", "1234567890", false)]
    [InlineData("Subdomain special characters", "tenant-1", "foo@tenant1.com", "1234567890", false)]
    [InlineData("Subdomain with spaces", "tenant 1", "foo@tenant1.com", "1234567890", false)]
    public async Task CreateTenantHandler_WhenValidatingCommand_ShouldValidateCorrectly(string name,
        string subdomain, string email,
        string phone, bool expected)
    {
        // Arrange
        var command = new CreateTenant.Command(name, subdomain, email, phone);

        // Act
        var result = await _mediator.Send(command);

        // Assert
        result.IsSuccess.Should().Be(expected);
        result.Errors?.Length.Should().Be(expected ? null : 1);
    }
}