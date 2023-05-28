using JetBrains.Annotations;

namespace PlatformPlatform.SharedKernel.DomainModeling.Validation;

[UsedImplicitly]
public sealed record AttributeError(string? AttributeName, string Message);