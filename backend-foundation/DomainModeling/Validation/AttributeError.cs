using JetBrains.Annotations;

namespace PlatformPlatform.Foundation.DomainModeling.Validation;

[UsedImplicitly]
public sealed record AttributeError(string? PropertyName, string Message);