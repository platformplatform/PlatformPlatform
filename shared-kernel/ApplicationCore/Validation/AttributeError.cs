using JetBrains.Annotations;

namespace PlatformPlatform.SharedKernel.ApplicationCore.Validation;

[UsedImplicitly]
public sealed record AttributeError(string? AttributeName, string Message);