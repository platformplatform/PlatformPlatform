using System.Net;
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

        var htmlBody = Render(htmlSource, template, true);
        var plainTextBody = Render(plainTextSource, template, false);

        var subject = ExtractSubject(htmlBody, template.Name);
        return new EmailRenderResult(subject, htmlBody, plainTextBody);
    }

    private string Render(string source, EmailTemplateBase template, bool htmlEscape)
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

        // The HTML and plaintext templates share a single source (the Value helper emits the same
        // {{ field | html_escape }} into both), but HTML escaping must only apply to the HTML body.
        // For the plaintext pass, shadow html_escape/e with identity so the .txt output carries the
        // raw text the sender typed rather than entity-encoded markup.
        if (!htmlEscape)
        {
            var plainTextOverrides = new ScriptObject();
            plainTextOverrides.Import("html_escape", (string? value) => value ?? "");
            plainTextOverrides.Import("e", (string? value) => value ?? "");
            context.PushGlobal(plainTextOverrides);
        }

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

        // The <title> content may be HTML-escaped (templates pipe untrusted subject text through
        // html_escape to prevent breaking out of <head>). The email Subject is a plaintext header, so
        // decode entities back to the raw text. A no-op for titles built from trusted, unescaped text.
        return WebUtility.HtmlDecode(WhitespaceRegex().Replace(match.Groups[1].Value, " ").Trim());
    }

    [GeneratedRegex("<title>(.*?)</title>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex TitleRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
