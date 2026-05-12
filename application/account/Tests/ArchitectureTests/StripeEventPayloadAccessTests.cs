using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace Account.Tests.ArchitectureTests;

/// <summary>
///     Enforces the rule that the webhook hot path makes zero reads of <c>stripe_events.payload</c>. The
///     durable archive column is consulted only by the admin disaster-recovery handler
///     <c>ReplayArchivedTenantStripeEvents</c>. Any new caller that legitimately needs to read the archive
///     must be added to the allowlist with a justification comment.
/// </summary>
public sealed class StripeEventPayloadAccessTests
{
    // Only these files may dereference `.Payload` on a persisted StripeEvent value. The aggregate itself
    // owns the column declaration; the EF mapping configures it; the write path stores it; and the
    // disaster-recovery handler is the one legitimate reader.
    private static readonly string[] AllowedFiles =
    [
        Path.Combine("Subscriptions", "Domain", "StripeEvent.cs"),
        Path.Combine("Subscriptions", "Domain", "StripeEventConfiguration.cs"),
        Path.Combine("Subscriptions", "Commands", "AcknowledgeStripeWebhook.cs"),
        Path.Combine("Tenants", "BackOffice", "Commands", "ReplayArchivedTenantStripeEvents.cs")
    ];

    // Match `.Payload` reads on a persisted StripeEvent symbol. The conventional variable names for
    // EF-materialized rows in this codebase are `stripeEvent`/`pendingEvent`/`pending`/`archived`/
    // `persisted`/`webhookEvent`/`recoveredEvent`. Explicitly excludes `StripeReplayEvent` reads (an
    // in-memory record carrying live events.list payloads) by checking the surrounding token does not
    // form `StripeReplayEvent.Payload`. We exclude the singular `stripeEvent` token because the
    // classifier hot loop uses it on `StripeReplayEvent`-typed values.
    private static readonly Regex PayloadAccessPattern = new(
        @"\b(?:StripeEvent|pendingEvent|pending|archived|persisted|webhookEvent|recoveredEvent)\.Payload\b",
        RegexOptions.Compiled
    );

    [Fact]
    public void ProductionCode_OnlyDisasterRecoveryHandler_MayReadStripeEventPayload()
    {
        // Arrange
        var coreFeaturesRoot = GetCoreFeaturesRoot();
        var allowedAbsolutePaths = AllowedFiles
            .Select(relative => Path.Combine(coreFeaturesRoot, relative))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Act
        var violations = new List<string>();
        foreach (var file in Directory.EnumerateFiles(coreFeaturesRoot, "*.cs", SearchOption.AllDirectories))
        {
            if (allowedAbsolutePaths.Contains(file)) continue;

            var content = File.ReadAllText(file);
            if (PayloadAccessPattern.IsMatch(content))
            {
                violations.Add(Path.GetRelativePath(coreFeaturesRoot, file));
            }
        }

        // Assert
        violations.Should().BeEmpty(
            "the webhook hot path makes zero reads of stripe_events.payload; only the disaster-recovery "
            + "handler ReplayArchivedTenantStripeEvents may consult the durable archive. "
            + $"Violations: {string.Join(", ", violations)}. "
            + "If a new caller legitimately needs to read the archive, add it to AllowedFiles with a justification comment."
        );
    }

    private static string GetCoreFeaturesRoot([CallerFilePath] string callerFilePath = "")
    {
        // Resolve via the test file's own path so the test always reads source from the same worktree it
        // was compiled in. The test file lives at
        // application/account/Tests/ArchitectureTests/StripeEventPayloadAccessTests.cs; Core/Features
        // sits four directories up and two down at application/account/Core/Features.
        var testDirectory = Path.GetDirectoryName(callerFilePath)!;
        return Path.GetFullPath(Path.Combine(testDirectory, "..", "..", "Core", "Features"));
    }
}
