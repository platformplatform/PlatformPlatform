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

    private async Task<int> Execute(string? language)
    {
        Prerequisite.Ensure(Prerequisite.Dotnet, Prerequisite.Docker);

        // TODO: Use IDisposable
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
        var ollamaApiClient = await GetTranslationClient();

        var poCatalog = await ReadTranslationFile(translationFile);

        AnsiConsole.MarkupLine($"Language detected: {poCatalog.Language}");

        var translationEntries = poCatalog.Values
            .Select(poEntry =>
                {
                    var translation = poCatalog.GetTranslation(poEntry.Key);
                    TranslationEntry translationEntry = string.IsNullOrWhiteSpace(translation) ? new NonTranslatedEntry(poEntry.Key) : new TranslatedEntry(poEntry.Key, translation);

                    return translationEntry;
                }
            )
            .ToArray();

        var translator = new Translator(new OllamaTranslationService(ollamaApiClient), poCatalog.Language);
        var translated = await translator.Translate(translationEntries);
        AnsiConsole.MarkupLine("[green]Translation completed.[/]");

        foreach (var translatedEntry in translated)
        {
            UpdateCatalogTranslation(poCatalog, translatedEntry);
        }

        await WriteTranslationFile(translationFile, poCatalog);
    }

    private static async Task<OllamaApiClient> GetTranslationClient()
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
            }
        );
        return ollamaApiClient;
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

    private static void UpdateCatalogTranslation(POCatalog poCatalog, TranslatedEntry translation)
    {
        var key = translation.Key;
        var poEntry = poCatalog[key];
        if (poEntry is POSingularEntry)
        {
            AnsiConsole.MarkupLine($"Singular {key.Id}");
            AnsiConsole.MarkupLine("Last message: " + translation.Translation);
            poCatalog.Remove(key);
            poCatalog.Add(new POSingularEntry(key) { Translation = translation.Translation });
        }
        else
        {
            throw new InvalidOperationException($"Plural is currently not supported. Key: '{key.Id}'");
        }
    }

    private record TranslatedEntry(POKey Key, string Translation) : TranslationEntry
    {
        public TranslatedEntry Reverse()
        {
            return new TranslatedEntry(new POKey(Translation, null, Key.ContextId), Key.Id);
        }
    }

    private record NonTranslatedEntry(POKey Key) : TranslationEntry;

    private record TranslationEntry;

    private class Translator
    {
        private readonly string _englishToTargetLanguagePrompt;
        private readonly string _targetLanguageToEnglishPrompt;
        private readonly ITranslationService _translationService;

        public Translator(ITranslationService translationService, string targetLanguage)
        {
            _translationService = translationService;
            _englishToTargetLanguagePrompt = CreatePrompt("English", targetLanguage);
            _targetLanguageToEnglishPrompt = CreatePrompt(targetLanguage, "English");
        }

        public async Task<IEnumerable<TranslatedEntry>> Translate(TranslationEntry[] translationEntries)
        {
            var translatedEntries = translationEntries.OfType<TranslatedEntry>().ToList();
            var nonTranslatedEntries = translationEntries.OfType<NonTranslatedEntry>().ToArray();
            AnsiConsole.MarkupLine($"Keys missing translation: {nonTranslatedEntries.Length}");
            if (nonTranslatedEntries.Length == 0)
            {
                AnsiConsole.MarkupLine("[green]Translation completed, nothing to translate.[/]");
            }

            var toReturn = new List<TranslatedEntry>();
            foreach (var nonTranslatedEntry in nonTranslatedEntries)
            {
                var translated = await TranslateSingleEntry(translatedEntries.AsReadOnly(), nonTranslatedEntry);
                translatedEntries.Add(translated);
                toReturn.Add(translated);
            }

            AnsiConsole.MarkupLine("[green]Translation completed.[/]");
            return toReturn;
        }

        private async Task<TranslatedEntry> TranslateSingleEntry(IReadOnlyCollection<TranslatedEntry> translatedEntries, NonTranslatedEntry nonTranslatedEntry)
        {
            AnsiConsole.MarkupLine($"[green]Translating {nonTranslatedEntry.Key.Id}[/]");

            TranslatedEntry translated = null!;
            TranslatedEntry reverseTranslated = null!;
            await AnsiConsole.Status().StartAsync("Initialize translation...", async context =>
                {
                    translated = await _translationService.Translate(_englishToTargetLanguagePrompt, translatedEntries, nonTranslatedEntry, context);

                    // translate back into the original language and check if translation matches
                    AnsiConsole.MarkupLine($"[yellow]Translated {nonTranslatedEntry.Key.Id} to {translated.Translation}. Checking translation..[/]");

                    var reverseTranslations = translatedEntries.Select(x => x.Reverse()).ToArray();
                    reverseTranslated = await _translationService.Translate(
                        _targetLanguageToEnglishPrompt,
                        reverseTranslations,
                        new NonTranslatedEntry(new POKey(translated.Translation, translated.Key.PluralId, translated.Key.ContextId)),
                        context
                    );
                }
            );

            if (reverseTranslated.Translation == translated.Key.Id)
            {
                AnsiConsole.MarkupLine("[yellow]Reverse translation is matching.[/]");
                return translated;
            }

            AnsiConsole.MarkupLine("[yellow]Reverse translation is not matching. [/]");
            if (AnsiConsole.Confirm($"Is translation {translated.Translation} acceptable?"))
            {
                AnsiConsole.MarkupLine("[yellow]Translation accepted.[/]");
                return translated;
            }

            // TODO: Ask the user for input
            throw new NotImplementedException();
        }

        private static string CreatePrompt(string sourceLanguage, string targetLanguage)
        {
            return $"""
                    You are a translation service translating from {sourceLanguage} to {targetLanguage}.
                    Return only the translation, not the original text or any other information.
                    Only the last message should be translated. Use previous messages to understand the context.
                    """;
        }
    }

    private interface ITranslationService
    {
        public Task<TranslatedEntry> Translate(string systemPrompt, IEnumerable<TranslatedEntry> existingTranslations, NonTranslatedEntry nonTranslatedEntry, StatusContext context);
    }

    private class OllamaTranslationService(IOllamaApiClient ollamaApiClient)
        : ITranslationService
    {
        public async Task<TranslatedEntry> Translate(string systemPrompt, IEnumerable<TranslatedEntry> existingTranslations, NonTranslatedEntry nonTranslatedEntry, StatusContext context)
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
                messages.Add(new Message(ChatRole.Assistant, translation.Translation));
            }

            messages.Add(new Message { Role = ChatRole.User, Content = nonTranslatedEntry.Key.Id });
            StringBuilder content = new();

            var response = (await ollamaApiClient.SendChat(
                new ChatRequest { Model = ModelName, Messages = messages },
                status =>
                {
                    content.Append(status?.Message.Content ?? "");
                    var percent = Math.Round(content.Length / (nonTranslatedEntry.Key.Id.Length * 1.2) * 100); // +20% is a guess
                    context.Status($"Translating {Math.Min(100, percent)}%");
                }
            )).ToList();

            var translated = response.Last();
            return new TranslatedEntry(nonTranslatedEntry.Key, translated.Content!);
        }
    }
}
