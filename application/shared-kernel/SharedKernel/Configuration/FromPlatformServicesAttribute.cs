namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Indicates that the parameter should be bound using the keyed service registered with the specified key.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public class FromPlatformServicesAttribute : FromKeyedServicesAttribute
{
    public const string PlatformServiceKey = "PlatformPlatform";
    /// <summary>
    /// Creates a new <see cref="FromPlatformServicesAttribute"/> instance.
    /// </summary>
    public FromPlatformServicesAttribute() : base(PlatformServiceKey)
    {
    }
}
