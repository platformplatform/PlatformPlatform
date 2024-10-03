using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Text;
using Karambolo.PO;
using OllamaSharp;
using OllamaSharp.Models.Chat;
using PlatformPlatform.DeveloperCli.Installation;
using PlatformPlatform.DeveloperCli.Utilities;
using Spectre.Console;

namespace PlatformPlatform.DeveloperCli.Commands;

public class TranslateCommand : Command
{
    private const string InstanceName = "platform-platform-ollama";
    private const string DockerImageName = "ollama/ollama";
    private const int Port = 11434;
    private const string ModelName = "llama2";

    public TranslateCommand() : base(
        "translate",
        $"Update language files with missing translations 🐡 (ALPHA) powered by {ModelName}"
    )
    {
        var languageOption = new Option<string?>(
            ["<language>", "--language", "-l"],
            "The name of the language to translate (e.g `da-DK`)"
        );

        AddOption(languageOption);

        Handler = CommandHandler.Create<string?>(Execute);
    }

    private static async Task<int> Execute(string? language)
    {
        Prerequisite.Ensure(Prerequisite.Dotnet, Prerequisite.Docker);

        try
        {
            var translationFile = GetTranslationFile(language);
            await RunTranslation(translationFile);
            return 0;
        }
        catch (Exception e)
        {
            AnsiConsole.MarkupLine($"[red]Translation failed. {e.Message}[/]");
            return 1;
        }
    }

    private static string GetTranslationFile(string? language)
    {
        var workingDirectory = new DirectoryInfo(Path.Combine(Configuration.GetSourceCodeFolder(), "..", "application"));
        var translationFiles = workingDirectory
            .GetFiles("*.po", SearchOption.AllDirectories)
            .Where(f => !f.FullName.Contains("node_modules") &&
                        !f.FullName.EndsWith("en-US.po") &&
                        !f.FullName.EndsWith("pseudo.po")
            )
            .ToDictionary(s => s.FullName.Replace(workingDirectory.FullName, ""), f => f);

        if (language is not null)
        {
            var translationFile = translationFiles.Values
                .FirstOrDefault(f => f.Name.Equals($"{language}.po", StringComparison.OrdinalIgnoreCase));

            return translationFile?.FullName
                   ?? throw new InvalidOperationException($"Translation file for language '{language}' not found.");
        }

        var prompt = new SelectionPrompt<string>()
            .Title("Please select the file to translate")
            .AddChoices(translationFiles.Keys);

        var selection = AnsiConsole.Prompt(prompt);
        return translationFiles[selection].FullName;
    }

    private static async Task RunTranslation(string translationFile)
    {
        var poCatalog = await ReadTranslationFile(translationFile);
        var entries = poCatalog.EnsureOnlySingularEntries();

        AnsiConsole.MarkupLine($"Language detected: {poCatalog.Language}");

        using var translationService = await OllamaTranslationService.Create();
        var translator = new Translator(translationService, poCatalog.Language);
        var translated = await translator.Translate(entries);

        if (!translated.Any())
        {
            return;
        }

        foreach (var translatedEntry in translated)
        {
            poCatalog.UpdateEntry(translatedEntry);
        }

        await WriteTranslationFile(translationFile, poCatalog);
    }

    private static async Task<POCatalog> ReadTranslationFile(string translationFile)
    {
        var translationContent = await File.ReadAllTextAsync(translationFile);
        var poParser = new POParser();
        var poParseResult = poParser.Parse(new StringReader(translationContent));
        if (!poParseResult.Success)
        {
            throw new InvalidOperationException($"Failed to parse PO file. {poParseResult.Diagnostics}");
        }

        if (poParseResult.Catalog.Language is null)
        {
            throw new InvalidOperationException($"Failed to parse PO file {translationFile}. Language not found.");
        }

        return poParseResult.Catalog;
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

    private sealed class Translator(ITranslationService translationService, string targetLanguage)
    {
        private readonly string _englishToTargetLanguagePrompt = CreatePrompt("English", targetLanguage);
        private readonly string _targetLanguageToEnglishPrompt = CreatePrompt(targetLanguage, "English");

        public async Task<IReadOnlyCollection<POSingularEntry>> Translate(IReadOnlyCollection<POSingularEntry> translationEntries)
        {
            var translatedEntries = translationEntries.Where(x => x.HasTranslation()).ToList();
            var nonTranslatedEntries = translationEntries.Where(x => !x.HasTranslation()).ToList();
            AnsiConsole.MarkupLine($"Keys missing translation: {nonTranslatedEntries.Count}");
            if (nonTranslatedEntries.Count == 0)
            {
                AnsiConsole.MarkupLine("[green]Translation completed, nothing to translate.[/]");
                return [];
            }

            var toReturn = new List<POSingularEntry>();
            foreach (var nonTranslatedEntry in nonTranslatedEntries)
            {
                var translated = await TranslateSingleEntry(translatedEntries.AsReadOnly(), nonTranslatedEntry);
                translatedEntries.Add(translated);
                toReturn.Add(translated);
            }

            AnsiConsole.MarkupLine("[green]Translation completed.[/]");
            return toReturn;
        }

        private async Task<POSingularEntry> TranslateSingleEntry(IReadOnlyCollection<POSingularEntry> translatedEntries, POSingularEntry nonTranslatedEntry)
        {
            AnsiConsole.MarkupLine($"Translating: [cyan]{nonTranslatedEntry.Key.Id}[/]");

            POSingularEntry translated = null!;
            POSingularEntry reverseTranslated = null!;
            await AnsiConsole.Status().StartAsync("Initialize translation...", async context =>
                {
                    translated = await translationService.Translate(_englishToTargetLanguagePrompt, translatedEntries, nonTranslatedEntry, context);

                    // translate back into the original language and check if translation matches
                    AnsiConsole.MarkupLine($"Translated to: [cyan]{translated.GetTranslation()}[/]");

                    AnsiConsole.MarkupLine("Checking translation...");
                    var reverseTranslations = translatedEntries.Select(x => x.Reverse()).ToArray();
                    reverseTranslated = await translationService.Translate(
                        _targetLanguageToEnglishPrompt,
                        reverseTranslations,
                        translated.Reverse(),
                        context
                    );
                }
            );

            if (reverseTranslated.GetTranslation() == translated.Key.Id)
            {
                AnsiConsole.MarkupLine("[green]Reverse translation is matching.[/]");
                return translated;
            }

            AnsiConsole.MarkupLine("[yellow]Reverse translation is not matching. [/]");
            if (AnsiConsole.Confirm("Is translation acceptable?"))
            {
                AnsiConsole.MarkupLine("[green]Translation accepted.[/]");
                return translated;
            }

            var userTranslation = AnsiConsole.Ask<string>("Please input your own translation.");
            if (string.IsNullOrWhiteSpace(userTranslation))
            {
                throw new InvalidOperationException("Invalid translation.");
            }

            return translated.ApplyTranslation(userTranslation);
        }

        private static string CreatePrompt(string sourceLanguage, string targetLanguage)
        {
            return $"""
                    You are a translation service translating from {sourceLanguage} to {targetLanguage}.
                    Return only the translation, not the original text or any other information.
                    """;
        }
    }

    private interface ITranslationService : IDisposable
    {
        public Task<POSingularEntry> Translate(string systemPrompt, IReadOnlyCollection<POSingularEntry> existingTranslations, POSingularEntry nonTranslatedEntry, StatusContext context);
    }

    private sealed class OllamaTranslationService : ITranslationService
    {
        private readonly DockerServer _dockerServer;
        private readonly IOllamaApiClient _ollamaApiClient;

        private OllamaTranslationService(IOllamaApiClient ollamaApiClient, DockerServer dockerServer)
        {
            _ollamaApiClient = ollamaApiClient;
            _dockerServer = dockerServer;
        }

        public async Task<POSingularEntry> Translate(string systemPrompt, IReadOnlyCollection<POSingularEntry> existingTranslations, POSingularEntry nonTranslatedEntry, StatusContext context)
        {
            var messages = new List<Message>
            {
                new()
                {
                    Role = ChatRole.System,
                    Content = systemPrompt
                }
            };

            foreach (var translation in existingTranslations)
            {
                messages.Add(new Message(ChatRole.User, translation.Key.Id));
                messages.Add(new Message(ChatRole.Assistant, translation.GetTranslation()));
            }

            messages.Add(new Message { Role = ChatRole.User, Content = nonTranslatedEntry.Key.Id });
            StringBuilder content = new();

            context.Status("Translating (thinking...)");

            var response = (await _ollamaApiClient.SendChat(
                new ChatRequest { Model = ModelName, Messages = messages },
                status =>
                {
                    content.Append(status?.Message.Content ?? "");
                    var percent = Math.Round(content.Length / (nonTranslatedEntry.Key.Id.Length * 1.2) * 100); // +20% is a guess
                    context.Status($"Translating {Math.Min(100, percent)}%");
                }
            )).ToList();

            context.Status("Translating 100%");

            var translated = response.Last().Content!;
            return nonTranslatedEntry.ApplyTranslation(translated);
        }

        public void Dispose()
        {
            _dockerServer.StopServer();
        }

        public static async Task<ITranslationService> Create()
        {
            var dockerServer = new DockerServer(DockerImageName, InstanceName, Port, "/root/.ollama");

            IOllamaApiClient ollamaApiClient;
            try
            {
                dockerServer.StartServer();
                ollamaApiClient = await GetTranslationClient();
            }
            catch
            {
                dockerServer.StopServer();
                throw;
            }

            return new OllamaTranslationService(ollamaApiClient, dockerServer);
        }

        private static async Task<OllamaApiClient> GetTranslationClient()
        {
            AnsiConsole.MarkupLine("[green]Connecting to Ollama API.[/]");
            var ollamaApiClient = new OllamaApiClient(
                new HttpClient { BaseAddress = new Uri($"http://localhost:{Port}"), Timeout = TimeSpan.FromMinutes(15) }
            );

            await AnsiConsole.Status().StartAsync("Checking base model...", async context =>
                {
                    var models = await ollamaApiClient.ListLocalModels();
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
                }
            );
            return ollamaApiClient;
        }
    }
}

public static class Extensions
{
    public static string GetTranslation(this POSingularEntry poEntry)
    {
        var translation = poEntry.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(translation))
        {
            throw new InvalidOperationException("No translation was found.");
        }

        return translation;
    }

    public static bool HasTranslation(this POSingularEntry poEntry)
    {
        return !string.IsNullOrWhiteSpace(poEntry.Translation);
    }

    public static POSingularEntry Reverse(this POSingularEntry poEntry)
    {
        var key = new POKey(poEntry.GetTranslation(), null, poEntry.Key.ContextId);
        var entry = new POSingularEntry(key)
        {
            Translation = poEntry.Key.Id,
            Comments = poEntry.Comments
        };

        return entry;
    }

    public static POSingularEntry ApplyTranslation(this POSingularEntry poEntry, string translation)
    {
        return new POSingularEntry(poEntry.Key)
        {
            Translation = translation,
            Comments = poEntry.Comments
        };
    }

    public static IReadOnlyCollection<POSingularEntry> EnsureOnlySingularEntries(this POCatalog catalog)
    {
        if (catalog.Values.Any(x => x is not POSingularEntry))
        {
            throw new NotSupportedException("Only single translations are supported.");
        }

        return catalog.Values.OfType<POSingularEntry>().ToArray();
    }

    public static void UpdateEntry(this POCatalog poCatalog, POSingularEntry translatedEntry)
    {
        var key = translatedEntry.Key;
        var poEntry = poCatalog[key];
        if (poEntry is POSingularEntry)
        {
            var index = poCatalog.IndexOf(poEntry);
            poCatalog.Remove(key);
            poCatalog.Insert(index, translatedEntry);
        }
        else
        {
            throw new InvalidOperationException($"Plural is currently not supported. Key: '{key.Id}'");
        }
    }
}
