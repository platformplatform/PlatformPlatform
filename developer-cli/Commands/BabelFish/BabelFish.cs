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
    private const string ModelName = "babelfish-po";
    private const string ModelFile = "po.Modelfile";
    private const string BaseModelName = "llama2";

    private readonly string _modelPath = Path.Combine(Environment.SolutionFolder, "Commands", "BabelFish", "Models");

    public BabelFish() : base("babel-fish", $"Your local translator 🐡 (ALPHA) powered by {BaseModelName}")
    {
        var fileArgument = new Argument<string>("file", "The file to translate");
        var translateCommand = new Command("translate", "Translate a file")
        {
            fileArgument
        };
        AddCommand(translateCommand);
        translateCommand.SetHandler(async file => { await Translate(file); }, fileArgument);
    }

    private async Task Translate(string sourceFile)
    {
        var workingDirectory = Path.Combine(
            Environment.SolutionFolder,
            "../application/account-management/WebApp/src/translations/locale"
        );
        var sourceFilePath = Path.Combine(workingDirectory, sourceFile);
        if (!File.Exists(sourceFilePath))
        {
            AnsiConsole.MarkupLine($"[red]File {sourceFile} not found.[/]");
            System.Environment.Exit(1);
        }

        using var server = new DockerServer(new DockerServerOptions
            {
                ImageName = DockerImageName,
                InstanceName = InstanceName,
                Ports = new Dictionary<string, string> { { "11434", "11434" } },
                Volumes = new Dictionary<string, string> { { InstanceName, "/root/.ollama" } }
            }
        );

        server.StartServer();
        await AnsiConsole.Status().StartAsync("Initializing translation...", async context =>
        {
            AnsiConsole.MarkupLine("[green]Connecting to Ollama API.[/]");
            var uri = new Uri("http://localhost:11434");
            var ollamaApiClient = new OllamaApiClient(uri);

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
            else
            {
                context.Status("Base model already downloaded.");
            }

            context.Status("Creating translation model.");
            var modelFilePath = Path.Combine(_modelPath, ModelFile);
            if (!File.Exists(modelFilePath))
            {
                context.Status($"Model file {ModelFile} not found.");
                throw new InvalidOperationException($"Model file '{ModelFile}' not found.");
            }

            await ollamaApiClient.CreateModel(
                ModelName,
                await File.ReadAllTextAsync(modelFilePath),
                status => context.Status($"{status.Status}")
            );
            AnsiConsole.MarkupLine($"[green]Model {ModelName} created.[/]");

            context.Status("Loading translation context into model.");

            context.Status("Initializing translation...");
            var sourceContent = await File.ReadAllTextAsync(sourceFilePath);
            var totalLines = sourceContent.Split("\n").Length + 3;
            var currentLine = 0;

            Message[] messages = [];
            try
            {
                var result = await ollamaApiClient.SendChat(new ChatRequest
                {
                    Model = ModelName,
                    Messages = new List<Message> { new() { Role = "user", Content = sourceContent } }
                }, status =>
                {
                    var content = status.Message?.Content ?? "";
                    currentLine += content.Count(c => c == '\n');
                    var percent = Math.Round((decimal)currentLine / totalLines * 100);
                    if (percent > 150) throw new InvalidOperationException("Translation failed.");

                    context.Status($"Translating file {Math.Min(100, percent)}%");
                });

                messages = result.ToArray();
            }
            catch (Exception e)
            {
                AnsiConsole.MarkupLine($"[red]Translation failed. {e.Message}[/]");
                System.Environment.Exit(1);
            }

            AnsiConsole.MarkupLine("[green]Translation completed.[/]");
            var lastResult = messages.LastOrDefault();
            AnsiConsole.MarkupLine($"Role: {lastResult?.Role}.");
            var translationContents = GetTranslationContents(lastResult?.Content ?? "");

            await File.WriteAllTextAsync(sourceFilePath, translationContents);
            AnsiConsole.MarkupLine($"[green]Translated file saved to {sourceFilePath}[/]");
            AnsiConsole.MarkupLine(
                "[yellow]WARNING: Please proofread translations, make sure the language is inclusive and polite.[/]"
            );
        });
    }

    private string GetTranslationContents(string responseContent)
    {
        var lines = responseContent.Split("\n");
        if (lines.Length >= 5 && lines[0].StartsWith("Language detected:") && lines.Last() == "'" && lines[1] == "'")
        {
            return string.Join("\n", lines.Skip(2).Take(lines.Length - 3));
        }

        AnsiConsole.MarkupLine("[red]Translation could not be read, unexpected format.[/]");
        return responseContent;
    }
}