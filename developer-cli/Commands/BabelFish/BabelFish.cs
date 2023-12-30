using System.CommandLine;
using JetBrains.Annotations;
using OllamaSharp;
using OllamaSharp.Models;
using PlatformPlatform.DeveloperCli.Utilities;
using Spectre.Console;
using Environment = PlatformPlatform.DeveloperCli.Installation.Environment;

namespace PlatformPlatform.DeveloperCli.Commands.BabelFish;

[UsedImplicitly]
public class BabelFish : Command
{
    private const string InstanceName = "babelfish";
    private const string DockerImageName = "ollama/ollama";
    private const int Port = 11434;
    private const string ModelName = "babelfish-po";
    private const string ModelFile = "po.Modelfile";
    private const string BaseModelName = "llama2";
    private const string LocalizationFilesPath = "../application/account-management/WebApp/src/translations/locale";

    private readonly string _modelPath = Path.Combine(Environment.SolutionFolder, "Commands", "BabelFish", "Models");

    public BabelFish() : base("babel-fish", $"Your local translator 🐡 (ALPHA) powered by {BaseModelName}")
    {
        var fileArgument = new Argument<string>("file", "The file to translate");
        var translateCommand = new Command("translate", "Translate a file")
        {
            fileArgument
        };
        AddCommand(translateCommand);
        translateCommand.SetHandler(async file => { await Execute(file); }, fileArgument);
    }

    private async Task Execute(string sourceFile)
    {
        var sourceFilePath = Path.Combine(Environment.SolutionFolder, LocalizationFilesPath, sourceFile);
        if (!File.Exists(sourceFilePath))
        {
            AnsiConsole.MarkupLine($"[red]File {sourceFile} not found.[/]");
            System.Environment.Exit(1);
        }

        var modelFilePath = Path.Combine(_modelPath, ModelFile);
        if (!File.Exists(modelFilePath))
        {
            AnsiConsole.MarkupLine($"[red]Model file {ModelFile} not found.[/]");
            System.Environment.Exit(1);
        }

        var dockerServer = new DockerServer(DockerImageName, InstanceName, Port, "/root/.ollama");
        try
        {
            dockerServer.StartServer();

            await Translate(modelFilePath, sourceFilePath);
        }
        catch (Exception e)
        {
            AnsiConsole.MarkupLine($"[red]Translation failed. {e.Message}[/]");
            System.Environment.ExitCode = 1;
        }
        finally
        {
            dockerServer.StopServer();
        }
    }

    private async Task Translate(string modelFilePath, string translationFile)
    {
        AnsiConsole.MarkupLine("[green]Connecting to Ollama API.[/]");
        var ollamaApiClient = new OllamaApiClient(
            new HttpClient { BaseAddress = new Uri($"http://localhost:{Port}"), Timeout = TimeSpan.FromMinutes(15) }
        );

        await AnsiConsole.Status().StartAsync("Checking base model...", async context =>
        {
            var models = (await ollamaApiClient.ListLocalModels()).ToArray();
            var baseModel = models.FirstOrDefault(m => m.Name.StartsWith($"{BaseModelName}:"));

            context.Status("Checking base model.");
            if (baseModel is null)
            {
                context.Status("Downloading base model.");
                await ollamaApiClient.PullModel(
                    BaseModelName,
                    status => context.Status($"({status.Percent}%) ## {status.Status}")
                );
                AnsiConsole.MarkupLine("[green]Base model downloaded.[/]");
            }
        });

        await AnsiConsole.Status().StartAsync("Creating translation model...", async context =>
        {
            AnsiConsole.MarkupLine($"[green]Creating {ModelName} model.[/]");
            await ollamaApiClient.CreateModel(
                ModelName,
                await File.ReadAllTextAsync(modelFilePath),
                status => context.Status($"{status.Status}")
            );
        });

        Message[] messages = [];
        await AnsiConsole.Status().StartAsync("Initialize translation...", async context =>
        {
            var translationContent = await File.ReadAllTextAsync(translationFile);
            var totalLines = translationContent.Split("\n").Length + 3;
            var currentLine = 0;

            var chatRequest = new ChatRequest
            {
                Model = ModelName,
                Messages = new List<Message> { new() { Role = "user", Content = translationContent } }
            };

            context.Status("Loading language model. This can take several minutes...");
            messages = (await ollamaApiClient.SendChat(chatRequest, status =>
            {
                var content = status.Message?.Content ?? "";
                currentLine += content.Count(c => c == '\n');
                var percent = Math.Round((decimal)currentLine / totalLines * 100);
                if (percent > 100) throw new InvalidOperationException($"Translation failed. {percent}%.");

                context.Status($"Translating file {Math.Min(100, percent)}%");
            })).ToArray();

            AnsiConsole.MarkupLine("[green]Translation completed.[/]");
        });

        var lastResult = messages.LastOrDefault();
        AnsiConsole.MarkupLine($"Role: {lastResult?.Role}.");
        var translationContents = GetTranslationContents(lastResult?.Content ?? "");

        await File.WriteAllTextAsync(translationFile, translationContents);
        AnsiConsole.MarkupLine($"[green]Translated file saved to {translationFile}[/]");
        AnsiConsole.MarkupLine(
            "[yellow]WARNING: Please proofread translations, make sure the language is inclusive and polite.[/]"
        );
    }

    private string GetTranslationContents(string responseContent)
    {
        var lines = responseContent.Split("\n");
        if (lines.Length >= 5 && lines[0].StartsWith("Language detected:") && lines.Last() == "'" && lines[1] == "'")
        {
            return string.Join("\n", lines.Skip(2).Take(lines.Length - 3));
        }

        throw new InvalidOperationException("Translation could not be read, unexpected format.");
    }
}