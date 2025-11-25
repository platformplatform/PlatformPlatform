using System.ClientModel;
using System.CommandLine;
using System.Text;
using Azure.AI.OpenAI;
using Karambolo.PO;
using Microsoft.Extensions.AI;
using OpenAI.Chat;
using PlatformPlatform.DeveloperCli.Installation;
using PlatformPlatform.DeveloperCli.Utilities;
using Spectre.Console;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

namespace PlatformPlatform.DeveloperCli.Commands;

public class TranslateCommand : Command
{
    public TranslateCommand() : base(
        "translate",
        $"Update language files with missing translations powered by {OpenAiTranslationService.ModelName}"
    )
    {
        var selfContainedSystemOption = new Option<string?>("--self-contained-system", "-s") { Description = "Translate only files in a specific self-contained system" };
        var languageOption = new Option<string?>("--language", "-l") { Description = "Translate only files for a specific language (e.g. da-DK)" };

        Options.Add(selfContainedSystemOption);
        Options.Add(languageOption);

        SetAction(async parseResult => await Execute(
                parseResult.GetValue(selfContainedSystemOption),
                parseResult.GetValue(languageOption)
            )
        );
    }

    private static async Task Execute(string? selfContainedSystem, string? language)
    {
        Prerequisite.Ensure(Prerequisite.Dotnet);

        try
        {
            var translationFiles = GetTranslationFiles(selfContainedSystem, language);
            await RunTranslation(translationFiles);
        }
        catch (Exception e)
        {
            AnsiConsole.MarkupLine($"[red]Translation failed. {e.Message}[/]");
            Environment.Exit(1);
        }
    }

    private static string[] GetTranslationFiles(string? selfContainedSystem, string? language)
    {
        var translationFiles = Directory.GetFiles(Configuration.ApplicationFolder, "*.po", SearchOption.AllDirectories)
            .Where(f => !f.Contains("node_modules") &&
                        !f.EndsWith("en-US.po") &&
                        !f.EndsWith("pseudo.po")
            )
            .ToDictionary(s => s.Replace(Configuration.ApplicationFolder, ""), f => f);

        if (selfContainedSystem is not null)
        {
            var availableSystems = Directory.GetDirectories(Configuration.ApplicationFolder)
                .Select(Path.GetFileName)
                .ToArray();

            if (!availableSystems.Contains(selfContainedSystem))
            {
                AnsiConsole.MarkupLine($"[red]ERROR:[/] Invalid self-contained system. Available systems are: {string.Join(", ", availableSystems)}");
                Environment.Exit(1);
            }

            translationFiles = translationFiles
                .Where(f => f.Key.StartsWith($"{Path.DirectorySeparatorChar}{selfContainedSystem}{Path.DirectorySeparatorChar}"))
                .ToDictionary(s => s.Key, f => f.Value);

            if (!translationFiles.Any())
            {
                AnsiConsole.MarkupLine($"[red]ERROR:[/] No translation files found in {selfContainedSystem}");
                Environment.Exit(1);
            }
        }

        if (language is not null)
        {
            translationFiles = translationFiles
                .Where(f => f.Key.EndsWith($"{language}.po"))
                .ToDictionary(s => s.Key, f => f.Value);

            if (!translationFiles.Any())
            {
                var systemInfo = selfContainedSystem != null ? $" in {selfContainedSystem}" : "";
                AnsiConsole.MarkupLine($"[red]ERROR:[/] No translation files found for language {language}{systemInfo}");
                Environment.Exit(1);
            }
        }

        return translationFiles.Values.ToArray();
    }

    private static async Task RunTranslation(string[] translationFiles)
    {
        var isAnyFileTranslated = false;
        var translationService = OpenAiTranslationService.Create();
        foreach (var translationFile in translationFiles)
        {
            var isTranslated = await RunTranslation(translationService, translationFile);
            isAnyFileTranslated = isAnyFileTranslated || isTranslated;
        }

        if (isAnyFileTranslated)
        {
            AnsiConsole.MarkupLine($"Total tokens used: [yellow]{translationService.UsageStatistics.TotalTokens}[/]. Translation cost: [yellow]${translationService.UsageStatistics.TotalCost:F4}[/]");
        }
    }

    private static async Task<bool> RunTranslation(OpenAiTranslationService translationService, string translationFile)
    {
        var poCatalog = await ReadTranslationFile(translationFile);
        var entries = poCatalog.EnsureOnlySingularEntries();

        AnsiConsole.MarkupLine($"Language detected: {poCatalog.Language}");
        var translator = new Translator(translationService, poCatalog.Language);
        var translated = await translator.Translate(entries);

        if (!translated.Any())
        {
            return false;
        }

        foreach (var translatedEntry in translated)
        {
            poCatalog.UpdateEntry(translatedEntry);
        }

        await WriteTranslationFile(translationFile, poCatalog);

        return true;
    }

    private static async Task<POCatalog> ReadTranslationFile(string translationFile)
    {
        var translationContent = await File.ReadAllTextAsync(translationFile);
        var poParser = new POParser();
        var poParseResult = poParser.Parse(new StringReader(translationContent));
        if (!poParseResult.Success)
        {
            AnsiConsole.MarkupLine($"[red]ERROR:[/] Failed to parse PO file. {poParseResult.Diagnostics}");
            Environment.Exit(1);
        }

        if (poParseResult.Catalog.Language is null)
        {
            AnsiConsole.MarkupLine($"[red]ERROR:[/] Failed to parse PO file {translationFile}. Language not found.");
            Environment.Exit(1);
        }

        return poParseResult.Catalog;
    }

    private static async Task WriteTranslationFile(string translationFile, POCatalog poCatalog)
    {
        var poGenerator = new POGenerator(new POGeneratorSettings { IgnoreEncoding = true, IgnoreLongLines = true });
        var fileStream = File.OpenWrite(translationFile);
        poGenerator.Generate(fileStream, poCatalog);
        await fileStream.FlushAsync();
        fileStream.Close();

        AnsiConsole.MarkupLine($"[green]Translated file saved to {translationFile}[/]");
        AnsiConsole.MarkupLine("[yellow]WARNING: Please proofread to make sure the language is inclusive.[/]");
    }

    private sealed class Translator(OpenAiTranslationService translationService, string targetLanguage)
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
                if (translated == null) // User chose to stop
                {
                    AnsiConsole.MarkupLine("[yellow]Translation process stopped. Saving changes collected so far.[/]");
                    break;
                }

                if (!translated.HasTranslation()) continue;
                translatedEntries.Add(translated);
                toReturn.Add(translated);
            }

            AnsiConsole.MarkupLine(toReturn.Count > 0
                ? $"[green]{toReturn.Count} entries have been translated.[/]"
                : "[yellow]No entries were translated.[/]"
            );

            return toReturn;
        }

        private async Task<POSingularEntry?> TranslateSingleEntry(
            IReadOnlyCollection<POSingularEntry> translatedEntries,
            POSingularEntry nonTranslatedEntry
        )
        {
            var currentPrompt = _englishToTargetLanguagePrompt;

            while (true)
            {
                AnsiConsole.MarkupLine($"Translating: [cyan]{nonTranslatedEntry.Key.Id}[/]");
                POSingularEntry translated = null!;
                POSingularEntry reverseTranslated = null!;
                await AnsiConsole.Status().StartAsync("Initialize translation...", async context =>
                    {
                        translated = await translationService.Translate(
                            currentPrompt, translatedEntries, nonTranslatedEntry, context
                        );

                        // Translate back into the original language and check if translation matches
                        AnsiConsole.MarkupLine($"Translated to: [cyan]{translated.GetTranslation()}[/]");

                        AnsiConsole.MarkupLine("Checking translation...");
                        var reverseTranslations = translatedEntries.Select(x => x.ReverseKeyAndTranslation()).ToArray();
                        reverseTranslated = await translationService.Translate(
                            _targetLanguageToEnglishPrompt,
                            reverseTranslations,
                            translated.ReverseKeyAndTranslation(),
                            context
                        );
                    }
                );

                if (string.Equals(reverseTranslated.GetTranslation(), translated.Key.Id, StringComparison.OrdinalIgnoreCase))
                {
                    AnsiConsole.MarkupLine("[green]Reverse translation is matching.[/]");
                    return translated;
                }

                AnsiConsole.MarkupLine($"[yellow]Reverse translation is not matching. Reverse translation is[/] [cyan]{reverseTranslated.GetTranslation()}[/]");

                var choice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("What would you like to do?")
                        .AddChoices("Accept translation", "Try again", "Provide context for retranslation", "Input own translation", "Skip", "Stop and save")
                );

                switch (choice)
                {
                    case "Accept translation":
                        AnsiConsole.MarkupLine("[green]Translation accepted.[/]");
                        return translated;
                    case "Try again":
                        AnsiConsole.MarkupLine("[green]Trying translation again...[/]");
                        continue;
                    case "Provide context for retranslation":
                        var context = AnsiConsole.Ask<string>("Please provide context for the translation:");
                        currentPrompt = _englishToTargetLanguagePrompt + $"\nAdditional context for translation: {context}";
                        AnsiConsole.MarkupLine("[green]Context added. Retranslating...[/]");
                        continue;
                    case "Input own translation":
                        var userTranslation = AnsiConsole.Ask<string>("Please input your own translation:");
                        if (string.IsNullOrWhiteSpace(userTranslation))
                        {
                            AnsiConsole.MarkupLine("[red]Invalid translation. Please try again.[/]");
                            continue;
                        }

                        return translated.ApplyTranslation(userTranslation);
                    case "Skip":
                        AnsiConsole.MarkupLine("[yellow]Translation skipped.[/]");
                        return translated.ApplyTranslation(string.Empty);
                    case "Stop and save":
                        AnsiConsole.MarkupLine("[yellow]Stopping translation process.[/]");
                        return null;
                }
            }
        }

        private static string CreatePrompt(string sourceLanguage, string targetLanguage)
        {
            return $"""
                    You are a translation service translating from {sourceLanguage} to {targetLanguage}.
                    Return only the translation, not the original text or any other information.
                    If the original text contains punctuation or special characters, it is very important to replicate them. Do not try to correct bad grammar in the original text.

                    E.g., if the original text is "enter **Your-Name* with no-more-than/#five%characters..!"
                    Then a translation to, e.g., Danish should be "Indtast **Dit-Navn* med maksimalt/#fem%bogstaver..!"
                    """;
        }
    }

    private sealed class OpenAiTranslationService(IChatClient chatClient)
    {
        public const string ModelName = "gpt-5-mini";
        public readonly Gpt5MiniUsageStatistics UsageStatistics = new();

        public static OpenAiTranslationService Create()
        {
            var (apiKey, endpoint) = GetApiKeyAndEndpoint();

            IChatClient chatClient;
            if (endpoint is null)
            {
                // Use standard OpenAI client for default endpoint
                chatClient = new ChatClient(ModelName, apiKey).AsIChatClient();
            }
            else
            {
                // Use Azure OpenAI client for custom endpoints
                var azureClient = new AzureOpenAIClient(new Uri(endpoint), new ApiKeyCredential(apiKey));
                chatClient = azureClient.GetChatClient(ModelName).AsIChatClient();
            }

            return new OpenAiTranslationService(chatClient);
        }

        private static (string apiKey, string? endpoint) GetApiKeyAndEndpoint()
        {
            const string apiKeySecretName = "OpenAIApiKey";
            const string endpointSecretName = "OpenAIEndpoint";

            var apiKey = SecretHelper.GetSecret(apiKeySecretName);
            var endpoint = SecretHelper.GetSecret(endpointSecretName);

            if (apiKey is not null)
            {
                return (apiKey, endpoint);
            }

            AnsiConsole.MarkupLine("OpenAI Key is missing.");
            apiKey = AnsiConsole.Prompt(
                new TextPrompt<string>("[yellow]Enter your OpenAI Key. Use a standard OpenAI key (sk-...) or an Azure OpenAI key[/]")
                    .Validate(key => key.Length >= 32 ? ValidationResult.Success() : ValidationResult.Error("Open AI Keys starts with 'sk-' and must be at least 51 characters long and Azure Open AI key must be 32 characters long."))
            );

            if (!apiKey.StartsWith("sk-"))
            {
                AnsiConsole.MarkupLine("[green]API Key is not a standard OpenAI key. Azure OpenAI key detected.[/]");
                endpoint = AnsiConsole.Prompt(
                    new TextPrompt<string>("[yellow]Please enter the Azure OpenAI endpoint URL (e.g. https://<your-resource-name>.openai.azure.com)[/]")
                        .Validate(url => Uri.TryCreate(url, UriKind.Absolute, out _))
                );
                SecretHelper.SetSecret(endpointSecretName, endpoint);
            }

            SecretHelper.SetSecret(apiKeySecretName, apiKey);

            return (apiKey, endpoint);
        }

        public async Task<POSingularEntry> Translate(
            string systemPrompt,
            IReadOnlyCollection<POSingularEntry> existingTranslations,
            POSingularEntry nonTranslatedEntry,
            StatusContext context
        )
        {
            var messages = new List<ChatMessage>
            {
                new(ChatRole.System, systemPrompt)
            };

            foreach (var translation in existingTranslations)
            {
                messages.Add(new ChatMessage(ChatRole.User, translation.Key.Id));
                messages.Add(new ChatMessage(ChatRole.Assistant, translation.GetTranslation()));
            }

            messages.Add(new ChatMessage(ChatRole.User, nonTranslatedEntry.Key.Id));
            context.Status("Translating (thinking...)");

            StringBuilder content = new();
            var streamingUpdates = new List<ChatResponseUpdate>();
            await foreach (var update in chatClient.GetStreamingResponseAsync(messages))
            {
                streamingUpdates.Add(update);
                content.Append(update.Text);
                var percent = Math.Round(content.Length / (nonTranslatedEntry.Key.Id.Length * 1.2) * 100); // +20% is a guess
                context.Status($"Translating {Math.Min(100, percent)}%");
            }

            // Get usage from the aggregated response
            var completedResponse = streamingUpdates.ToChatResponse();
            if (completedResponse.Usage is not null)
            {
                UsageStatistics.Update(completedResponse.Usage);
            }

            context.Status("Translating 100%");

            var translated = content.ToString();
            return nonTranslatedEntry.ApplyTranslation(translated);
        }

        public record Gpt5MiniUsageStatistics
        {
            private const decimal InputPricePerThousandTokens = 0.00025m;
            private const decimal OutputPricePerThousandTokens = 0.002m;

            private long _inputTokenCount;
            private long _outputTokenCount;

            public decimal TotalCost
            {
                get
                {
                    var inputCost = _inputTokenCount / 1000m * InputPricePerThousandTokens;
                    var outputCost = _outputTokenCount / 1000m * OutputPricePerThousandTokens;

                    return inputCost + outputCost;
                }
            }

            public long TotalTokens => _inputTokenCount + _outputTokenCount;

            public void Update(UsageDetails usage)
            {
                _inputTokenCount += usage.InputTokenCount ?? 0;
                _outputTokenCount += usage.OutputTokenCount ?? 0;
            }
        }
    }
}

public static class Extensions
{
    extension(POSingularEntry poEntry)
    {
        public string GetTranslation()
        {
            var translation = poEntry.FirstOrDefault();
            if (string.IsNullOrWhiteSpace(translation))
            {
                throw new InvalidOperationException("No translation was found.");
            }

            return translation;
        }

        public bool HasTranslation()
        {
            return !string.IsNullOrWhiteSpace(poEntry.Translation);
        }

        public POSingularEntry ReverseKeyAndTranslation()
        {
            var key = new POKey(poEntry.GetTranslation(), null, poEntry.Key.ContextId);
            var entry = new POSingularEntry(key)
            {
                Translation = poEntry.Key.Id,
                Comments = poEntry.Comments
            };

            return entry;
        }

        public POSingularEntry ApplyTranslation(string translation)
        {
            return new POSingularEntry(poEntry.Key)
            {
                Translation = translation,
                Comments = poEntry.Comments
            };
        }
    }

    extension(POCatalog catalog)
    {
        public IReadOnlyCollection<POSingularEntry> EnsureOnlySingularEntries()
        {
            if (catalog.Values.Any(x => x is not POSingularEntry))
            {
                throw new NotSupportedException("Only single translations are supported.");
            }

            return catalog.Values.OfType<POSingularEntry>().ToArray();
        }

        public void UpdateEntry(POSingularEntry translatedEntry)
        {
            var key = translatedEntry.Key;
            var poEntry = catalog[key];
            if (poEntry is POSingularEntry)
            {
                var index = catalog.IndexOf(poEntry);
                catalog.Remove(key);
                catalog.Insert(index, translatedEntry);
            }
            else
            {
                throw new InvalidOperationException($"Plural is currently not supported. Key: '{key.Id}'");
            }
        }
    }
}
