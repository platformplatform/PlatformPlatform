using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using JetBrains.Annotations;
using Karambolo.PO;
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
    private readonly string _modelPath = Path.Combine(Environment.SolutionFolder, "Commands", "BabelFish", "Models");

    public BabelFish() : base("babel-fish", $"Your local translator 🐡 (ALPHA) powered by {BaseModelName}")
    {
        var fileOption = new Option<string?>(
            ["<language>", "--language", "-l"],
            "The name of the language to translate (e.g `da-DK`)"
        );

        AddOption(fileOption);

        Handler = CommandHandler.Create<string?>(Execute);
    }

    private async Task<int> Execute(string? language)
    {
        var modelFilePath = Path.Combine(_modelPath, ModelFile);
        if (!File.Exists(modelFilePath))
        {
            AnsiConsole.MarkupLine($"[red]Model file {ModelFile} not found.[/]");
            System.Environment.Exit(1);
        }

        var translationFile = GetTranslationFile(language);

        var dockerServer = new DockerServer(DockerImageName, InstanceName, Port, "/root/.ollama");
        try
        {
            dockerServer.StartServer();

            await Translate(modelFilePath, translationFile);

            return 0;
        }
        catch (Exception e)
        {
            AnsiConsole.MarkupLine($"[red]Translation failed. {e.Message}[/]");
            return 1;
        }
        finally
        {
            dockerServer.StopServer();
        }
    }

    private string GetTranslationFile(string? language)
    {
        var workingDirectory = new DirectoryInfo(Path.Combine(Environment.SolutionFolder, "..", "application"));
        var translationFiles = workingDirectory
            .GetFiles("*.po", SearchOption.AllDirectories)
            .Where(f => !f.FullName.Contains("node_modules"))
            .Where(f => !f.FullName.EndsWith("en-US.po"))
            .ToDictionary(s => s.FullName.Replace(workingDirectory.FullName, ""), f => f);

        if (language is not null)
        {
            var translationFile = translationFiles.Values
                .FirstOrDefault(f => f.Name.Equals($"{language}.po", StringComparison.OrdinalIgnoreCase));

            if (translationFile is not null) return translationFile.FullName;

            AnsiConsole.MarkupLine($"[red]ERROR: Translation file for language '{language}' not found.[/]");
            System.Environment.Exit(1);
        }

        var prompt = new SelectionPrompt<string>()
            .Title("Please select the file to translate")
            .AddChoices(translationFiles.Keys);

        var selection = AnsiConsole.Prompt(prompt);
        return translationFiles[selection].FullName;
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

        var poParseResult = await ReadTranslationFile(translationFile);

        var poCatalog = poParseResult.Catalog;
        var language = poCatalog.Language;
        AnsiConsole.MarkupLine($"Language detected: {language}");

        await AnsiConsole.Status().StartAsync("Initialize translation...", async context =>
        {
            var keysMissingTranslation = new List<POKey>();
            var messages = new List<Message>
            {
                new()
                {
                    Role = "system",
                    Content = $"""
                               You are a translation service translating from English to {language}.
                               Return only the translation, not the original text or any other information.
                               """
                }
            };

            foreach (var key in poCatalog.Keys)
            {
                var translation = poCatalog.GetTranslation(key);
                if (translation.Length == 0)
                {
                    keysMissingTranslation.Add(key);
                }
                else
                {
                    messages.Add(new Message { Role = "user", Content = key.Id });
                    messages.Add(new Message { Role = "assistant", Content = translation });
                }
            }

            AnsiConsole.MarkupLine($"Keys missing translation: {keysMissingTranslation.Count}");
            if (keysMissingTranslation.Count == 0)
            {
                AnsiConsole.MarkupLine("[green]Translation completed, nothing to translate.[/]");
                return;
            }

            var translationCount = 0;
            foreach (var key in keysMissingTranslation)
            {
                AnsiConsole.MarkupLine($"[green]Translating {key.Id}[/]");
                var percent = Math.Round((decimal)translationCount / keysMissingTranslation.Count * 100);
                var totalContentLength = (decimal)Math.Round(key.Id.Length * 1.2); // 20% overhead
                var contentLength = 0;

                context.Status($"Translating {Math.Min(100, percent)}% (thinking...)");

                messages.Add(new Message { Role = "user", Content = key.Id });

                var result = await ollamaApiClient.SendChat(new ChatRequest
                {
                    Model = BaseModelName,
                    Messages = messages
                }, status =>
                {
                    contentLength += status.Message?.Content?.Length ?? 0;
                    var contentPercent = Math.Round(contentLength / totalContentLength * 100);
                    var pad = "".PadRight((int)Math.Max(0, Math.Round(contentPercent / 10 - 10)), '.');

                    context.Status($"Translating {Math.Min(100, percent)}% ({Math.Min(100, contentPercent)}%){pad}");
                });

                var lastMessage = result.Last();
                messages.Add(lastMessage);

                UpdateCatalogTranslation(poCatalog, key, lastMessage.Content);

                translationCount++;
            }

            AnsiConsole.MarkupLine("[green]Translation completed.[/]");

            await WriteTranslationFile(translationFile, poCatalog);
        });
    }

    private static void UpdateCatalogTranslation(POCatalog poCatalog, POKey key, string translation)
    {
        var entry = poCatalog[key];
        if (entry is POSingularEntry)
        {
            AnsiConsole.MarkupLine($"Singular {key.Id}");
            AnsiConsole.MarkupLine("Last message: " + translation);
            poCatalog.Remove(key);
            poCatalog.Add(new POSingularEntry(key)
            {
                Translation = translation
            });
        }
        else
        {
            AnsiConsole.MarkupLine($"[red]Plural is currently not supported Key: {key.Id}[/]");
            System.Environment.Exit(1);
        }
    }

    private static async Task WriteTranslationFile(string translationFile, POCatalog poCatalog)
    {
        var generate = new POGenerator(new POGeneratorSettings
        {
            IgnoreEncoding = true
        });
        var fileStream = File.OpenWrite(translationFile);
        generate.Generate(fileStream, poCatalog);
        await fileStream.FlushAsync();
        fileStream.Close();

        AnsiConsole.MarkupLine($"[green]Translated file saved to {translationFile}[/]");
        AnsiConsole.MarkupLine(
            "[yellow]WARNING: Please proofread translations, make sure the language is inclusive and polite.[/]"
        );
    }

    private static async Task<POParseResult> ReadTranslationFile(string translationFile)
    {
        var translationContent = await File.ReadAllTextAsync(translationFile);
        var parser = new POParser();
        var poParseResult = parser.Parse(new StringReader(translationContent));
        if (poParseResult.Success == false)
        {
            AnsiConsole.MarkupLine($"[red]Failed to parse PO file. {poParseResult.Diagnostics}[/]");
            System.Environment.Exit(1);
        }

        if (poParseResult.Catalog.Language is null)
        {
            AnsiConsole.MarkupLine("[red]Failed to parse PO file. Language not found.[/]");
            System.Environment.Exit(1);
        }

        return poParseResult;
    }
}