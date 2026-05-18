using System.Text.Json;

namespace SharedKernel.Configuration;

// Resolves the Docker volume name prefix that the Aspire AppHost stamps onto its persistent
// volumes and that the developer CLI's stop command uses to find and remove them. Both must agree,
// so the prefix is read in one place from development.dockerVolumePrefix in platform-settings.jsonc.
//
// Read straight off the checkout rather than through SharedKernel.Platform.Settings: that loader
// uses an embedded resource for the deployed backend, but the AppHost and the CLI always run from
// a repo checkout (and the CLI does not embed the file), so a plain file read is simpler and
// always reflects the current branch -- which matters because parallel git worktrees can sit on
// branches with different brands.
public static class DockerVolumeNaming
{
    public static string ResolveVolumePrefix()
    {
        return ResolveVolumePrefix(PortAllocation.FindRepositoryRoot(AppContext.BaseDirectory));
    }

    public static string ResolveVolumePrefix(string repositoryRoot)
    {
        var settingsPath = Path.Combine(repositoryRoot, "application", "platform-settings.jsonc");

        using var stream = File.OpenRead(settingsPath);
        using var document = JsonDocument.Parse(stream, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip });

        var prefix = document.RootElement.GetProperty("development").GetProperty("dockerVolumePrefix").GetString();
        if (string.IsNullOrWhiteSpace(prefix))
        {
            throw new InvalidOperationException($"'{settingsPath}' is missing a non-empty development.dockerVolumePrefix.");
        }

        return prefix;
    }
}
