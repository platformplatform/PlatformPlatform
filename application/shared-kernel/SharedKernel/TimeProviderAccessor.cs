namespace PlatformPlatform.SharedKernel;

/// <summary>
///     Provides access to the current TimeProvider using AsyncLocal for thread-safe, async-context-isolated access.
///     This allows domain entities to get the current time during construction while remaining testable.
///     Each async context (including parallel tests) has its own isolated TimeProvider instance.
/// </summary>
public static class TimeProviderAccessor
{
    private static readonly AsyncLocal<TimeProvider> CurrentTimeProvider = new();

    public static TimeProvider Current
    {
        get => CurrentTimeProvider.Value ?? TimeProvider.System;
        set => CurrentTimeProvider.Value = value;
    }
}
