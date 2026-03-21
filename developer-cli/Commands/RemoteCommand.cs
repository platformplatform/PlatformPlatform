using System.CommandLine;
using System.Diagnostics;
using DeveloperCli.Installation;
using DeveloperCli.Utilities;
using Spectre.Console;

namespace DeveloperCli.Commands;

public class RemoteCommand : Command
{
    private static readonly string SshConfigPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".ssh", "config");

    public RemoteCommand() : base("remote", "SSH into remote machines or manage SSH hosts")
    {
        var hostArgument = new Argument<string?>("host") { Description = "Host name to connect to", Arity = ArgumentArity.ZeroOrOne };
        var addOption = new Option<string?>("--add") { Description = "Add a new host (format: name@ip or name@hostname)" };
        var removeOption = new Option<string?>("--remove") { Description = "Remove a host" };
        var listOption = new Option<bool>("--list", "-l") { Description = "List all configured hosts" };

        Arguments.Add(hostArgument);
        Options.Add(addOption);
        Options.Add(removeOption);
        Options.Add(listOption);

        SetAction(parseResult => Execute(
                parseResult.GetValue(hostArgument),
                parseResult.GetValue(addOption),
                parseResult.GetValue(removeOption),
                parseResult.GetValue(listOption)
            )
        );
    }

    private static void Execute(string? host, string? add, string? remove, bool list)
    {
        if (add is not null)
        {
            AddHost(add);
            return;
        }

        if (remove is not null)
        {
            RemoveHost(remove);
            return;
        }

        if (list)
        {
            ListHosts();
            return;
        }

        if (host is not null)
        {
            ConnectToHost(host);
            return;
        }

        // Combine SSH config hosts with discovered VMs
        var hosts = GetConfiguredHosts();
        var discoveredVms = DiscoverVirtualMachines();

        // Merge: discovered VMs that aren't already in SSH config
        var allHosts = hosts.ToList();
        foreach (var vm in discoveredVms)
        {
            if (!allHosts.Any(h => h.HostName == vm.IpAddress || h.Name.Equals(vm.Name, StringComparison.OrdinalIgnoreCase)))
            {
                allHosts.Add(new SshHost(vm.Name, vm.IpAddress, Environment.UserName));
            }
        }

        if (allHosts.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No remote hosts found.[/]");
            var hostSpec = AnsiConsole.Ask<string>("Enter [green]name@ip[/] to add a host (e.g., my-vm@10.211.55.5):");
            AddHost(hostSpec);
            allHosts = GetConfiguredHosts().ToList();
            if (allHosts.Count == 0) return;
        }

        var selected = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select a host to connect to:")
                .AddChoices(allHosts.Select(h => $"{h.Name} ({h.HostName})"))
        );

        var selectedHost = allHosts.First(h => selected == $"{h.Name} ({h.HostName})");
        ConnectToHost(selectedHost.Name, selectedHost.HostName);
    }

    private static void AddHost(string hostSpec)
    {
        var parts = hostSpec.Split('@');
        if (parts.Length != 2)
        {
            AnsiConsole.MarkupLine("[red]Invalid format. Use: --add name@ip (e.g., --add my-vm@10.211.55.5)[/]");
            return;
        }

        var name = parts[0];
        var hostname = parts[1];
        var user = Environment.UserName;

        var hosts = GetConfiguredHosts();
        if (hosts.Any(h => h.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            AnsiConsole.MarkupLine($"[yellow]Host '{name}' already exists. Remove it first with --remove {name}[/]");
            return;
        }

        var sshDirectory = Path.GetDirectoryName(SshConfigPath)!;
        if (!Directory.Exists(sshDirectory)) Directory.CreateDirectory(sshDirectory);

        var entry = $"""

                     Host {name}
                         HostName {hostname}
                         User {user}
                     """;

        File.AppendAllText(SshConfigPath, entry + Environment.NewLine);
        AnsiConsole.MarkupLine($"[green]Added host '{name}' ({user}@{hostname})[/]");

        // Copy SSH key if available
        var publicKeyPath = Path.Combine(sshDirectory, "id_ed25519.pub");
        if (File.Exists(publicKeyPath))
        {
            AnsiConsole.MarkupLine("[blue]Copying SSH key to remote host...[/]");
            var copyKeyCommand = Configuration.IsWindows
                ? $"type \"{publicKeyPath}\" | ssh {name} \"mkdir -p ~/.ssh && cat >> ~/.ssh/authorized_keys\""
                : $"ssh-copy-id {name}";

            ProcessHelper.StartProcess(copyKeyCommand, exitOnError: false);
        }
        else
        {
            AnsiConsole.MarkupLine("[yellow]No SSH key found. You may need to enter a password when connecting.[/]");
        }
    }

    private static void RemoveHost(string name)
    {
        if (!File.Exists(SshConfigPath))
        {
            AnsiConsole.MarkupLine("[yellow]No SSH config file found.[/]");
            return;
        }

        var lines = File.ReadAllLines(SshConfigPath).ToList();
        var startIndex = -1;
        var endIndex = -1;

        for (var i = 0; i < lines.Count; i++)
        {
            if (lines[i].Trim().Equals($"Host {name}", StringComparison.OrdinalIgnoreCase))
            {
                startIndex = i;
                // Find the end of this host block (next Host line or end of file)
                for (var j = i + 1; j < lines.Count; j++)
                {
                    if (lines[j].TrimStart().StartsWith("Host ", StringComparison.OrdinalIgnoreCase))
                    {
                        endIndex = j;
                        break;
                    }
                }

                endIndex = endIndex == -1 ? lines.Count : endIndex;
                break;
            }
        }

        if (startIndex == -1)
        {
            AnsiConsole.MarkupLine($"[yellow]Host '{name}' not found in SSH config.[/]");
            return;
        }

        // Remove blank line before the host block if present
        if (startIndex > 0 && string.IsNullOrWhiteSpace(lines[startIndex - 1]))
        {
            startIndex--;
        }

        lines.RemoveRange(startIndex, endIndex - startIndex);
        File.WriteAllLines(SshConfigPath, lines);
        AnsiConsole.MarkupLine($"[green]Removed host '{name}'[/]");
    }

    private static void ListHosts()
    {
        var hosts = GetConfiguredHosts();
        if (hosts.Length == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No remote hosts configured. Use --add name@ip to add one.[/]");
            return;
        }

        var table = new Table();
        table.AddColumn("Name");
        table.AddColumn("Host");
        table.AddColumn("User");

        foreach (var host in hosts)
        {
            table.AddRow(host.Name, host.HostName, host.User);
        }

        AnsiConsole.Write(table);
    }

    private static void ConnectToHost(string name, string? hostname = null)
    {
        if (hostname is null)
        {
            var hosts = GetConfiguredHosts();
            var host = hosts.FirstOrDefault(h => h.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (host is null)
            {
                AnsiConsole.MarkupLine($"[red]Host '{name}' not found. Use --list to see configured hosts or --add to add one.[/]");
                return;
            }

            hostname = host.HostName;
            AnsiConsole.MarkupLine($"[blue]Connecting to {host.Name} ({host.User}@{hostname})...[/]");
        }
        else
        {
            AnsiConsole.MarkupLine($"[blue]Connecting to {name} ({Environment.UserName}@{hostname})...[/]");
        }

        var sshTarget = GetConfiguredHosts().Any(h => h.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            ? name
            : $"{Environment.UserName}@{hostname}";

        // Copy SSH key to remote host on first connect (enables passwordless login)
        var sshDirectory = Path.GetDirectoryName(SshConfigPath)!;
        var publicKeyPath = Path.Combine(sshDirectory, "id_ed25519.pub");
        if (File.Exists(publicKeyPath))
        {
            var checkResult = ProcessHelper.StartProcess($"ssh -o BatchMode=yes -o ConnectTimeout=5 {sshTarget} echo ok", redirectOutput: true, exitOnError: false).Trim();
            if (checkResult != "ok")
            {
                AnsiConsole.MarkupLine("[yellow]Copying SSH key to enable passwordless login...[/]");
                var copyCommand = Configuration.IsWindows
                    ? $"type \"{publicKeyPath}\" | ssh {sshTarget} \"mkdir -p ~/.ssh && cat >> ~/.ssh/authorized_keys\""
                    : $"ssh-copy-id {sshTarget}";
                ProcessHelper.StartProcess(copyCommand, exitOnError: false);
            }
        }

        var process = Process.Start(new ProcessStartInfo
            {
                FileName = "ssh",
                Arguments = $"-o SetEnv=TERM=xterm-256color {sshTarget}",
                UseShellExecute = false
            }
        );

        process?.WaitForExit();
    }

    private static SshHost[] GetConfiguredHosts()
    {
        if (!File.Exists(SshConfigPath)) return [];

        var lines = File.ReadAllLines(SshConfigPath);
        var hosts = new List<SshHost>();
        string? currentName = null;
        string? currentHostName = null;
        string? currentUser = null;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("Host ", StringComparison.OrdinalIgnoreCase) && !trimmed.Contains('*'))
            {
                if (currentName is not null)
                {
                    hosts.Add(new SshHost(currentName, currentHostName ?? currentName, currentUser ?? Environment.UserName));
                }

                currentName = trimmed["Host ".Length..].Trim();
                currentHostName = null;
                currentUser = null;
            }
            else if (trimmed.StartsWith("HostName ", StringComparison.OrdinalIgnoreCase))
            {
                currentHostName = trimmed["HostName ".Length..].Trim();
            }
            else if (trimmed.StartsWith("User ", StringComparison.OrdinalIgnoreCase))
            {
                currentUser = trimmed["User ".Length..].Trim();
            }
        }

        if (currentName is not null)
        {
            hosts.Add(new SshHost(currentName, currentHostName ?? currentName, currentUser ?? Environment.UserName));
        }

        return hosts.ToArray();
    }

    private static DiscoveredVm[] DiscoverVirtualMachines()
    {
        if (!Configuration.IsMacOs) return [];

        // Check if Parallels CLI is available
        var prlctlCheck = ProcessHelper.StartProcess("which prlctl", redirectOutput: true, exitOnError: false).Trim();
        if (string.IsNullOrEmpty(prlctlCheck)) return [];

        var vms = new List<DiscoveredVm>();
        var simpleOutput = ProcessHelper.StartProcess("prlctl list -a --no-header", redirectOutput: true, exitOnError: false).Trim();
        if (string.IsNullOrEmpty(simpleOutput)) return [];

        foreach (var line in simpleOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            // Format: {UUID}  STATUS  IP  NAME (columns are space-separated, name may contain spaces)
            var parts = line.Split([' '], 4, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 4) continue;
            var status = parts[1];
            if (!status.Equals("running", StringComparison.OrdinalIgnoreCase)) continue;

            var vmName = parts[3].Trim();
            var hostName = vmName.ToLower().Replace(' ', '-');

            // Get the VM's IP address via prlctl exec
            var ipOutput = ProcessHelper.StartProcess($"prlctl exec \"{vmName}\" hostname -I", redirectOutput: true, exitOnError: false).Trim();
            if (string.IsNullOrEmpty(ipOutput)) continue;

            var ipAddress = ipOutput.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            if (!string.IsNullOrEmpty(ipAddress) && !ipAddress.Contains(':'))
            {
                vms.Add(new DiscoveredVm(hostName, ipAddress));
            }
        }

        return vms.ToArray();
    }


    private sealed record DiscoveredVm(string Name, string IpAddress);

    private sealed record SshHost(string Name, string HostName, string User);
}
