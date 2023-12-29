using System.CommandLine;
using OllamaSharp;
using OllamaSharp.Models;
using PlatformPlatform.DeveloperCli.Installation;
using PlatformPlatform.DeveloperCli.Utilities;
using Spectre.Console;

namespace PlatformPlatform.DeveloperCli.Commands.BabelFish;

public class BabelFish : Command
{
    private const string InstanceName = "babelfish";
    private const string DockerImageName = "ollama/ollama";
    private const string ModelName = "babelfish-po";
    private const string ModelFile = "po.Modelfile";
    private const string BaseModelName = "llama2";

    private readonly string _modelPath =
        Path.Combine(AliasRegistration.SolutionFolder, "Commands", "BabelFish", "Models");

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
        var workingDirectory = Directory.GetCurrentDirectory();
        var sourceFilePath = Path.Combine(workingDirectory, sourceFile);
        if (!File.Exists(sourceFilePath)) AnsiConsole.MarkupLine($"[red]File {sourceFile} not found[/]");

        Console.WriteLine(AliasRegistration.SolutionFolder);


        using (new DockerServer(new DockerServerOptions
               {
                   ImageName = DockerImageName,
                   InstanceName = InstanceName,
                   WorkingDirectory = workingDirectory,
                   Ports = new Dictionary<string, string> { { "11434", "11434" } },
                   Volumes = new Dictionary<string, string> { { InstanceName, "/root/.ollama" } }
               }))
        {
            await AnsiConsole.Status().StartAsync("Initializing translation...", async context =>
            {
                AnsiConsole.MarkupLine("[green]Connecting to Ollama API[/]");
                var uri = new Uri("http://localhost:11434");
                var ollama = new OllamaApiClient(uri);

                var models = (await ollama.ListLocalModels()).ToArray();
                var baseModel = models.FirstOrDefault(m => m.Name.StartsWith($"{BaseModelName}:"));

                context.Status("Checking base model");
                if (baseModel == null)
                {
                    context.Status("Downloading base model");
                    await ollama.PullModel(BaseModelName,
                        status => context.Status($"({status.Percent}%) ## {status.Status}"));
                    AnsiConsole.MarkupLine("[green]Base model downloaded[/]");
                }
                else
                {
                    context.Status("Base model already downloaded");
                }

                context.Status("Creating translation model");
                var modelFilePath = Path.Combine(_modelPath, ModelFile);
                if (!File.Exists(modelFilePath))
                {
                    context.Status($"Model file {ModelFile} not found");
                    throw new Exception("Model file not found");
                }

                await ollama.CreateModel(ModelName, await File.ReadAllTextAsync(modelFilePath),
                    status => context.Status($"{status.Status}"));
                AnsiConsole.MarkupLine($"[green]Model {ModelName} created[/]");

                context.Status("Loading translation context into model");

                context.Status("Initializing translation...");
                var sourceContent = await File.ReadAllTextAsync(sourceFilePath);
                var totalLines = sourceContent.Split("\n").Length + 3;
                var currentLine = 0;

                var result = await ollama.SendChat(new ChatRequest
                {
                    Model = ModelName,
                    Messages = new List<Message>
                    {
                        new()
                        {
                            Role = "user",
                            Content = sourceContent
                        }
                    }
                }, status =>
                {
                    var content = status.Message?.Content ?? "";
                    currentLine += content.Count(c => c == '\n');
                    var percent = Math.Round((decimal)currentLine / totalLines * 100);
                    if (percent > 150) throw new Exception("Translation failed");

                    context.Status($"Translating file {Math.Min(100, percent)}%");
                });
                AnsiConsole.MarkupLine("[green]Translation completed[/]");
                var lastResult = result.ToArray().LastOrDefault();
                Console.WriteLine($"Role: {lastResult?.Role}");
                var text = GetTranslationContents(lastResult?.Content ?? "");

                await File.WriteAllTextAsync(sourceFilePath, text);
                AnsiConsole.MarkupLine($"[green]Translated file saved to {sourceFilePath}[/]");
                AnsiConsole.MarkupLine(
                    "[yellow]WARNING: Please proofread translations, make sure the language is inclusive and polite[/]");
            });
        }
    }

    private string GetTranslationContents(string responseContent)
    {
        var lines = responseContent.Split("\n");
        if (lines.Length >= 5 && lines[0].StartsWith("Language detected:") && lines.Last() == "'" && lines[1] == "'")
            return string.Join("\n", lines.Skip(2).Take(lines.Length - 3));
        AnsiConsole.MarkupLine("[red]Translation could not be read, unexpected format[/]");
        return responseContent;
    }
}