using System.Text.RegularExpressions;
using Scriban;
using Scriban.Runtime;
using SharedKernel.Platform;

namespace SharedKernel.Emails;

// Scriban-based renderer. Compiles each template at render time (templates are typically loaded from
// disk in dev and cached in prod via FileSystemEmailTemplateLoader). Subject is extracted from the
// rendered HTML's <title> element so authors only need to define it once in the template.
public sealed partial class ScribanEmailRenderer(ScriptObject helpers, IEmailTemplateLoader templateLoader) : IEmailRenderer
{
    public EmailRenderResult RenderEmail(EmailTemplateBase template)
    {
        var htmlSource = templateLoader.LoadHtml(template.Name, template.Locale);
        var plainTextSource = templateLoader.LoadPlainText(template.Name, template.Locale);

        var htmlBody = Render(htmlSource, template);
        var plainTextBody = Render(plainTextSource, template);

        var subject = ExtractSubject(htmlBody, template.Name);
        return new EmailRenderResult(subject, htmlBody, plainTextBody);
    }

    private string Render(string source, EmailTemplateBase template)
    {
        var parsed = Template.Parse(source);
        if (parsed.HasErrors)
        {
            throw new InvalidOperationException($"Email template '{template.Name}' has Scriban parse errors: {parsed.Messages}");
        }

        var modelObject = new ScriptObject();
        // Preserve property names verbatim (e.g., OtpCode stays OtpCode) so templates can reference
        // the same identifiers used in the C# model. Scriban's default member renamer converts
        // PascalCase to snake_case, which would silently break {{ OtpCode }} → empty.
        modelObject.Import(template.Model, renamer: member => member.Name);

        // {{ Tagline }} is locale-specific (the mail-channel tagline can differ per locale), so it
        // is pushed per render rather than baked into the shared helpers ScriptObject.
        var perRenderGlobals = new ScriptObject();
        perRenderGlobals.SetValue("Tagline", ResolveMailTagline(template.Locale), true);

        var context = new TemplateContext();
        context.PushGlobal(helpers);
        context.PushGlobal(perRenderGlobals);
        context.PushGlobal(modelObject);
        return parsed.Render(context);
    }

    private static string ResolveMailTagline(string locale)
    {
        var mailTaglines = Settings.Current.Branding.Tagline.Mail;
        if (mailTaglines.TryGetValue(locale, out var tagline))
        {
            return tagline;
        }

        throw new InvalidOperationException(
            $"platform-settings.jsonc: branding.tagline.mail does not include locale '{locale}'. Available: [{string.Join(", ", mailTaglines.Keys)}]."
        );
    }

    private static string ExtractSubject(string htmlBody, string templateName)
    {
        var match = TitleRegex().Match(htmlBody);
        if (!match.Success)
        {
            throw new InvalidOperationException($"Email template '{templateName}' is missing a <title> element required for the subject line.");
        }

        return WhitespaceRegex().Replace(match.Groups[1].Value, " ").Trim();
    }

    [GeneratedRegex("<title>(.*?)</title>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex TitleRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
