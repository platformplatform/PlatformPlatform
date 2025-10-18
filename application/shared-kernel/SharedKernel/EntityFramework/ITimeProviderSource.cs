namespace PlatformPlatform.SharedKernel.EntityFramework;

/// <summary>
///     Exposes TimeProvider for use in Entity Framework Core interceptors without knowing the concrete DbContext type.
/// </summary>
internal interface ITimeProviderSource
{
    TimeProvider TimeProvider { get; }
}
