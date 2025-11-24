using Xunit.Abstractions;
using Xunit.Sdk;

namespace PlatformPlatform.SharedKernel.Tests;

/// <summary>
///     Categorizes a test for conditional execution.
///     Common categories: "Noisy" (verbose output), "RequiresDocker", "RequiresAzure", "Integration", etc.
///     Use --exclude-category in the Developer CLI to filter out specific test categories.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
[TraitDiscoverer("PlatformPlatform.SharedKernel.Tests.TestCategoryDiscoverer", "PlatformPlatform.SharedKernel.Tests")]
public sealed class TestCategoryAttribute(string category) : Attribute, ITraitAttribute
{
    public string Category { get; } = category;
}

public class TestCategoryDiscoverer : ITraitDiscoverer
{
    public IEnumerable<KeyValuePair<string, string>> GetTraits(IAttributeInfo traitAttribute)
    {
        var category = traitAttribute.GetNamedArgument<string>(nameof(TestCategoryAttribute.Category));
        yield return new KeyValuePair<string, string>("Category", category);
    }
}
