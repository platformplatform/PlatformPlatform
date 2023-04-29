namespace PlatformPlatform.AccountManagement.Application.Tenants.Commands;

public interface ITenantCommand
{
    string Name { get; }

    string Email { get; }

    string Phone { get; }
}