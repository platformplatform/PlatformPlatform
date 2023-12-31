using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using JetBrains.Annotations;
using Karambolo.PO;
using OllamaSharp;
using OllamaSharp.Models;
using PlatformPlatform.DeveloperCli.Utilities;
using Spectre.Console;
using Environment = PlatformPlatform.DeveloperCli.Installation.Environment;

namespace PlatformPlatform.DeveloperCli.Commands;

[UsedImplicitly]
public class Translate : Command
{
    private const string InstanceName = "platform-platform-ollama";
    private const string DockerImageName = "ollama/ollama";
    private const int Port = 11434;
    private const string ModelName = "llama2";

    public Translate() : base("translate", $"Your local translator 🐡 (ALPHA) powered by {ModelName}")
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
        var dockerServer = new DockerServer(DockerImageName, InstanceName, Port, "/root/.ollama");
        try
        {
            var translationFile = GetTranslationFile(language);

            dockerServer.StartServer();

            await RunTranslation(translationFile);

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
            .Where(f => !f.FullName.Contains("node_modules") &&
                        !f.FullName.EndsWith("en-US.po") &&
                        !f.FullName.EndsWith("pseudo.po"))
            .ToDictionary(s => s.FullName.Replace(workingDirectory.FullName, ""), f => f);

        if (language is not null)
        {
            var translationFile = translationFiles.Values
                .FirstOrDefault(f => f.Name.Equals($"{language}.po", StringComparison.OrdinalIgnoreCase));

            return translationFile?.FullName ??
                   throw new InvalidOperationException($"Translation file for language '{language}' not found.");
        }

        var prompt = new SelectionPrompt<string>()
            .Title("Please select the file to translate")
            .AddChoices(translationFiles.Keys);

        var selection = AnsiConsole.Prompt(prompt);
        return translationFiles[selection].FullName;
    }

    private async Task RunTranslation(string translationFile)
    {
        AnsiConsole.MarkupLine("[green]Connecting to Ollama API.[/]");
        var ollamaApiClient = new OllamaApiClient(
            new HttpClient { BaseAddress = new Uri($"http://localhost:{Port}"), Timeout = TimeSpan.FromMinutes(15) }
        );

        await AnsiConsole.Status().StartAsync("Checking base model...", async context =>
        {
            var models = (await ollamaApiClient.ListLocalModels()).ToArray();
            var baseModel = models.FirstOrDefault(m => m.Name.StartsWith($"{ModelName}:"));

            context.Status("Checking base model.");
            if (baseModel is null)
            {
                context.Status("Downloading base model.");
                await ollamaApiClient.PullModel(
                    ModelName,
                    status => context.Status($"({status.Percent}%) ## {status.Status}")
                );
                AnsiConsole.MarkupLine("[green]Base model downloaded.[/]");
            }
        });

        var poParseResult = await ReadTranslationFile(translationFile);

        var poCatalog = poParseResult.Catalog;
        AnsiConsole.MarkupLine($"Language detected: {poCatalog.Language}");

        await AnsiConsole.Status().StartAsync("Initialize translation...", async context =>
        {
            var missingTranslations = new List<POKey>();
            var messages = new List<Message>
            {
                new()
                {
                    Role = "system",
                    Content = $"""
                               You are a translation service translating from English to {poCatalog.Language}.
                               Return only the translation, not the original text or any other information.
                               """
                }
            };

            foreach (var key in poCatalog.Keys)
            {
                var translation = poCatalog.GetTranslation(key);
                if (translation.Length == 0)
                {
                    missingTranslations.Add(key);
                }
                else
                {
                    messages.Add(new Message { Role = "user", Content = key.Id });
                    messages.Add(new Message { Role = "assistant", Content = translation });
                }
            }

            AnsiConsole.MarkupLine($"Keys missing translation: {missingTranslations.Count}");
            if (missingTranslations.Count == 0)
            {
                AnsiConsole.MarkupLine("[green]Translation completed, nothing to translate.[/]");
                return;
            }

            for (var index = 0; index < missingTranslations.Count; index++)
            {
                var key = missingTranslations[index];
                var content = "";

                AnsiConsole.MarkupLine($"[green]Translating {key.Id}[/]");
                context.Status($"Translating {index + 1}/{missingTranslations.Count} (thinking...)");

                messages.Add(new Message { Role = "user", Content = key.Id });

                messages = (await ollamaApiClient.SendChat(new ChatRequest
                {
                    Model = ModelName,
                    Messages = messages
                }, status =>
                {
                    content += status.Message?.Content ?? "";
                    var percent = Math.Round(content.Length / (key.Id.Length * 1.2) * 100); // +20% is a guess

                    context.Status($"Translating {index + 1}/{missingTranslations.Count} ({Math.Min(100, percent)}%)");
                })).ToList();

                UpdateCatalogTranslation(poCatalog, key, messages.Last().Content);
            }

            AnsiConsole.MarkupLine("[green]Translation completed.[/]");

            await WriteTranslationFile(translationFile, poCatalog);
        });
    }

    private static async Task<POParseResult> ReadTranslationFile(string translationFile)
    {
        var translationContent = await File.ReadAllTextAsync(translationFile);
        var poParser = new POParser();
        var poParseResult = poParser.Parse(new StringReader(translationContent));
        if (poParseResult.Success == false)
            throw new InvalidOperationException($"Failed to parse PO file. {poParseResult.Diagnostics}");

        if (poParseResult.Catalog.Language is null)
            throw new InvalidOperationException($"Failed to parse PO file {translationFile}. Language not found.");

        return poParseResult;
    }

    private static async Task WriteTranslationFile(string translationFile, POCatalog poCatalog)
    {
        var poGenerator = new POGenerator(new POGeneratorSettings { IgnoreEncoding = true });
        var fileStream = File.OpenWrite(translationFile);
        poGenerator.Generate(fileStream, poCatalog);
        await fileStream.FlushAsync();
        fileStream.Close();

        AnsiConsole.MarkupLine($"[green]Translated file saved to {translationFile}[/]");
        AnsiConsole.MarkupLine("[yellow]WARNING: Please proofread to make sure the language is inclusive.[/]");
    }

    private static void UpdateCatalogTranslation(POCatalog poCatalog, POKey key, string translation)
    {
        var poEntry = poCatalog[key];
        if (poEntry is POSingularEntry)
        {
            AnsiConsole.MarkupLine($"Singular {key.Id}");
            AnsiConsole.MarkupLine("Last message: " + translation);
            poCatalog.Remove(key);
            poCatalog.Add(new POSingularEntry(key) { Translation = translation });
        }
        else
        {
            throw new InvalidOperationException($"Plural is currently not supported. Key: '{key.Id}'");
        }
    }
}