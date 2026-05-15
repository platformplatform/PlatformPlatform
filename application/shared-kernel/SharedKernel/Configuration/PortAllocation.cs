using System.Net;
using System.Net.Sockets;

namespace SharedKernel.Configuration;

// Single source of truth for the local development port allocation. Reads .workspace/port.txt
// (single integer, whitespace-tolerant). If the file is missing, self-bootstraps: the root
// checkout gets the default base port; a worktree scans a fixed list of candidate base ports
// and picks the first one whose ports are all free locally. Once written, port.txt is the
// authoritative allocation for the lifetime of the checkout. Throws on a present-but-invalid file.
public sealed record PortAllocation(int BasePort)
{
    private const int DefaultBasePort = 9000;

    private const string WorkspaceDirectoryName = ".workspace";

    private const string PortFileName = "port.txt";

    // Worktrees scan these in order and pick the first base port whose full allocation is free.
    internal static readonly int[] WorktreeCandidateBasePorts = [9100, 9200, 9300, 9400, 9500, 9600, 9700, 9800, 9900];

    public int AppGateway => BasePort;

    public int Aspire => BasePort + 1;

    public int Postgres => BasePort + 2;

    public int Blob => BasePort + 3;

    public int MailpitSmtp => BasePort + 4;

    public int MailpitHttp => BasePort + 5;

    public int OtelEndpoint => BasePort + 8;

    public int ResourceService => BasePort + 9;

    public int MainApi => BasePort + 10;

    public int MainStatic => BasePort + 11;

    public int MainWorkers => BasePort + 12;

    public int AccountApi => BasePort + 13;

    public int AccountStatic => BasePort + 14;

    public int AccountWorkers => BasePort + 15;

    public int BackOfficeStatic => BasePort + 16;

    public int BackOfficeApi => BasePort + 17;

    public int[] AllPorts =>
    [
        AppGateway, Aspire, Postgres, Blob, MailpitSmtp, MailpitHttp, OtelEndpoint, ResourceService,
        MainApi, MainStatic, MainWorkers, AccountApi, AccountStatic, AccountWorkers, BackOfficeStatic, BackOfficeApi
    ];

    // Empty on the default base port so existing developers' Docker volumes are reused unchanged on
    // upgrade; non-empty on any other base port so parallel Aspire stacks (typically run from
    // separate git worktrees with different .workspace/port.txt values) get isolated volumes
    // instead of corrupting each other's state.
    public string VolumeNameInfix => BasePort == DefaultBasePort ? string.Empty : $"-{BasePort}";

    // Resolves the repository root by walking up from AppContext.BaseDirectory looking for the
    // .git folder (always present in a checked-out repo, including git worktrees where .git is a
    // file pointing at the real worktree directory), then reads or bootstraps .workspace/port.txt.
    // Use LoadFrom(repositoryRoot) when AppContext.BaseDirectory is outside the repo (e.g., a
    // single-file CLI binary published to the user's home directory).
    public static PortAllocation Load()
    {
        return LoadFrom(FindRepositoryRoot(AppContext.BaseDirectory));
    }

    public static PortAllocation LoadFrom(string repositoryRoot)
    {
        var workspaceDirectory = Path.Combine(repositoryRoot, WorkspaceDirectoryName);
        var portFilePath = Path.Combine(workspaceDirectory, PortFileName);

        // Treat "file doesn't exist" and "file exists but is empty" as the same condition: both
        // mean we need to (re-)bootstrap. The empty case happens when a concurrent caller has
        // opened the file with O_CREAT|O_TRUNC but hasn't written yet — overwriting it with the
        // same deterministic value is a harmless no-op.
        var content = File.Exists(portFilePath) ? File.ReadAllText(portFilePath).Trim() : "";

        if (content.Length == 0)
        {
            var bootstrapPort = IsWorktree(repositoryRoot)
                ? FindFreeBasePortForWorktree()
                : DefaultBasePort;
            Directory.CreateDirectory(workspaceDirectory);
            File.WriteAllText(portFilePath, $"{bootstrapPort}{Environment.NewLine}");
            return new PortAllocation(bootstrapPort);
        }

        if (!int.TryParse(content, out var basePort) || basePort <= 0)
        {
            throw new InvalidOperationException(
                $"Port allocation file '{portFilePath}' must contain a positive integer. Got: '{content}'."
            );
        }

        return new PortAllocation(basePort);
    }

    // True if .workspace/port.txt already exists -- distinguishes a fresh checkout from a configured one.
    public static bool PortFileExists(string repositoryRoot)
    {
        var portFilePath = Path.Combine(repositoryRoot, WorkspaceDirectoryName, PortFileName);
        return File.Exists(portFilePath);
    }

    // .git is a directory in the root checkout and a file in git worktrees.
    private static bool IsWorktree(string repositoryRoot)
    {
        return File.Exists(Path.Combine(repositoryRoot, ".git"));
    }

    private static int FindFreeBasePortForWorktree()
    {
        foreach (var candidate in WorktreeCandidateBasePorts)
        {
            var allocation = new PortAllocation(candidate);
            if (allocation.AllPorts.All(IsTcpPortFree)) return candidate;
        }

        throw new InvalidOperationException(
            $"No free base port available for this worktree. Tried: {string.Join(", ", WorktreeCandidateBasePorts)}."
        );
    }

    private static bool IsTcpPortFree(int port)
    {
        try
        {
            var listener = new TcpListener(IPAddress.Loopback, port);
            listener.Start();
            listener.Stop();
            return true;
        }
        catch (SocketException)
        {
            return false;
        }
    }

    private static string FindRepositoryRoot(string startDirectory)
    {
        var current = new DirectoryInfo(startDirectory);
        while (current is not null)
        {
            // .git is a directory in normal checkouts and a file in git worktrees -- both count.
            var gitMarker = Path.Combine(current.FullName, ".git");
            if (Directory.Exists(gitMarker) || File.Exists(gitMarker)) return current.FullName;
            current = current.Parent;
        }

        throw new InvalidOperationException(
            $"Could not locate the repository root walking up from '{startDirectory}'. Expected a '.git' directory or file."
        );
    }
}
