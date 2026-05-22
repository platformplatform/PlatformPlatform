using FluentAssertions;
using Scriban.Runtime;
using SharedKernel.Emails;
using SharedKernel.Platform;
using Xunit;

namespace SharedKernel.Tests.Emails;

public sealed class ScribanEmailRendererTests
{
    private readonly ScriptObject _helpers = EmailHelpers.CreateScriptObject("https://app.platformplatform.net");

    private ScribanEmailRenderer CreateRenderer(string html, string plainText)
    {
        return new ScribanEmailRenderer(_helpers, new InMemoryEmailTemplateLoader(html, plainText));
    }

    [Fact]
    public void RenderEmail_WhenTemplateHasVariables_ShouldSubstituteFromModel()
    {
        // Arrange
        var html = "<html><head><title>Hello {{ name }}</title></head><body><p>Welcome, {{ name }}!</p></body></html>";
        var plainText = "Welcome, {{ name }}!";
        var renderer = CreateRenderer(html, plainText);
        var template = new TestTemplate("welcome", "en-US", new { name = "Alice" });

        // Act
        var result = renderer.RenderEmail(template);

        // Assert
        result.Subject.Should().Be("Hello Alice");
        result.HtmlBody.Should().Contain("Welcome, Alice!");
        result.PlainTextBody.Should().Be("Welcome, Alice!");
    }

    [Fact]
    public void RenderEmail_WhenTemplateHasLoop_ShouldRenderEachItem()
    {
        // Arrange
        var html = "<html><head><title>Order</title></head><body><ul>{{ for item in items }}<li>{{ item }}</li>{{ end }}</ul></body></html>";
        var plainText = "{{ for item in items }}- {{ item }}\n{{ end }}";
        var renderer = CreateRenderer(html, plainText);
        var template = new TestTemplate("order", "en-US", new { items = new[] { "Apple", "Banana", "Cherry" } });

        // Act
        var result = renderer.RenderEmail(template);

        // Assert
        result.HtmlBody.Should().Contain("<li>Apple</li>").And.Contain("<li>Banana</li>").And.Contain("<li>Cherry</li>");
        result.PlainTextBody.Should().Contain("- Apple").And.Contain("- Banana").And.Contain("- Cherry");
    }

    [Fact]
    public void RenderEmail_WhenTemplateHasConditional_ShouldRenderBranch()
    {
        // Arrange
        var html = "<html><head><title>Receipt</title></head><body>{{ if paid }}Thank you!{{ else }}Please pay.{{ end }}</body></html>";
        var plainText = "{{ if paid }}Thank you!{{ else }}Please pay.{{ end }}";
        var renderer = CreateRenderer(html, plainText);

        // Act
        var paidResult = renderer.RenderEmail(new TestTemplate("receipt", "en-US", new { paid = true }));
        var unpaidResult = renderer.RenderEmail(new TestTemplate("receipt", "en-US", new { paid = false }));

        // Assert
        paidResult.HtmlBody.Should().Contain("Thank you!").And.NotContain("Please pay.");
        paidResult.PlainTextBody.Should().Be("Thank you!");
        unpaidResult.HtmlBody.Should().Contain("Please pay.").And.NotContain("Thank you!");
        unpaidResult.PlainTextBody.Should().Be("Please pay.");
    }

    [Fact]
    public void RenderEmail_WhenFormatCurrencyHelperUsedInEnUs_ShouldFormatWithDollar()
    {
        // Arrange
        var html = "<html><head><title>Invoice</title></head><body>{{ amount | format_currency \"USD\" \"en-US\" }}</body></html>";
        var renderer = CreateRenderer(html, "{{ amount | format_currency \"USD\" \"en-US\" }}");
        var template = new TestTemplate("invoice", "en-US", new { amount = 1234.56m });

        // Act
        var result = renderer.RenderEmail(template);

        // Assert
        result.PlainTextBody.Should().Be("$1,234.56");
    }

    [Fact]
    public void RenderEmail_WhenFormatCurrencyHelperUsedInDaDk_ShouldFormatWithKronerAndDanishGrouping()
    {
        // Arrange
        var html = "<html><head><title>Faktura</title></head><body>{{ amount | format_currency \"DKK\" \"da-DK\" }}</body></html>";
        var renderer = CreateRenderer(html, "{{ amount | format_currency \"DKK\" \"da-DK\" }}");
        var template = new TestTemplate("faktura", "da-DK", new { amount = 1234.56m });

        // Act
        var result = renderer.RenderEmail(template);

        // Assert
        result.PlainTextBody.Should().Contain("1.234,56").And.Contain("kr");
    }

    [Fact]
    public void RenderEmail_WhenFormatDateHelperUsed_ShouldFormatPerLocale()
    {
        // Arrange
        var date = new DateTimeOffset(2026, 5, 3, 10, 0, 0, TimeSpan.Zero);
        var html = "<html><head><title>Date</title></head><body>{{ date | format_date \"en-US\" }}</body></html>";
        var plainText = "{{ date | format_date \"da-DK\" }}";
        var renderer = CreateRenderer(html, plainText);
        var template = new TestTemplate("date", "en-US", new { date });

        // Act
        var result = renderer.RenderEmail(template);

        // Assert
        result.HtmlBody.Should().Contain("Sunday, May 3, 2026");
        result.PlainTextBody.Should().Contain("3. maj 2026");
    }

    [Fact]
    public void RenderEmail_WhenFormatDateHelperWithCustomFormat_ShouldHonorFormat()
    {
        // Arrange
        var date = new DateTimeOffset(2026, 5, 3, 10, 0, 0, TimeSpan.Zero);
        var html = "<html><head><title>Date</title></head><body>{{ date | format_date \"en-US\" \"yyyy-MM-dd\" }}</body></html>";
        var renderer = CreateRenderer(html, "{{ date | format_date \"en-US\" \"yyyy-MM-dd\" }}");
        var template = new TestTemplate("date", "en-US", new { date });

        // Act
        var result = renderer.RenderEmail(template);

        // Assert
        result.PlainTextBody.Should().Be("2026-05-03");
    }

    [Theory]
    [InlineData(0, "items")]
    [InlineData(1, "item")]
    [InlineData(2, "items")]
    [InlineData(7, "items")]
    public void RenderEmail_WhenPluralizeHelperUsed_ShouldPickCorrectForm(int count, string expected)
    {
        // Arrange
        var html = "<html><head><title>Cart</title></head><body>{{ count | pluralize \"item\" }}</body></html>";
        var renderer = CreateRenderer(html, "{{ count | pluralize \"item\" }}");
        var template = new TestTemplate("cart", "en-US", new { count });

        // Act
        var result = renderer.RenderEmail(template);

        // Assert
        result.PlainTextBody.Should().Be(expected);
    }

    [Fact]
    public void RenderEmail_WhenPluralizeHelperWithExplicitPlural_ShouldUseProvidedPlural()
    {
        // Arrange
        var html = "<html><head><title>Cart</title></head><body>{{ count | pluralize \"child\" \"children\" }}</body></html>";
        var renderer = CreateRenderer(html, "{{ count | pluralize \"child\" \"children\" }}");

        // Act
        var single = renderer.RenderEmail(new TestTemplate("cart", "en-US", new { count = 1 }));
        var many = renderer.RenderEmail(new TestTemplate("cart", "en-US", new { count = 3 }));

        // Assert
        single.PlainTextBody.Should().Be("child");
        many.PlainTextBody.Should().Be("children");
    }

    [Fact]
    public void RenderEmail_WhenHtmlAndPlainTextShareModel_ShouldProduceConsistentValues()
    {
        // Arrange: a finance-style template using all three helpers in both formats.
        var html = "<html><head><title>Receipt for {{ customer }}</title></head><body><p>{{ customer }}, you owe {{ amount | format_currency \"USD\" \"en-US\" }} due on {{ dueDate | format_date \"en-US\" \"yyyy-MM-dd\" }} ({{ lineItemCount | pluralize \"item\" }}).</p></body></html>";
        var plainText = "{{ customer }}, you owe {{ amount | format_currency \"USD\" \"en-US\" }} due on {{ dueDate | format_date \"en-US\" \"yyyy-MM-dd\" }} ({{ lineItemCount | pluralize \"item\" }}).";
        var renderer = CreateRenderer(html, plainText);
        var template = new TestTemplate("receipt", "en-US", new
            {
                customer = "Bob",
                amount = 99.95m,
                dueDate = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero),
                lineItemCount = 3
            }
        );

        // Act
        var result = renderer.RenderEmail(template);

        // Assert: both rendered bodies must contain the same dynamic values so plaintext stays in lockstep with HTML.
        const string expectedFragment = "Bob, you owe $99.95 due on 2026-06-01 (items).";
        result.HtmlBody.Should().Contain(expectedFragment);
        result.PlainTextBody.Should().Be(expectedFragment);
    }

    [Fact]
    public void RenderEmail_WhenHtmlEscapeHelperUsed_ShouldEscapeInHtmlButLeavePlaintextRaw()
    {
        // Scriban does no auto-escaping, so untrusted text interpolated into the HTML body must be
        // piped through html_escape. The same {{ field | html_escape }} appears in both the HTML and
        // plaintext sources (the Value JSX helper emits to both), so the renderer must escape only the
        // HTML pass and leave the plaintext raw — otherwise legitimate text like "you & me" would show
        // as "you &amp; me" in the .txt body.
        var html = "<html><head><title>Re: {{ subject | html_escape }}</title></head><body><span>{{ body | html_escape }}</span></body></html>";
        var plainText = "{{ body | html_escape }}";
        var renderer = CreateRenderer(html, plainText);
        var template = new TestTemplate("support", "en-US", new
            {
                subject = "Help</title><h1>Spoofed</h1>",
                body = "<script>alert('xss')</script> you & me"
            }
        );

        // Act
        var result = renderer.RenderEmail(template);

        // Assert
        result.HtmlBody.Should().Contain("&lt;script&gt;alert(&#39;xss&#39;)&lt;/script&gt; you &amp; me");
        result.HtmlBody.Should().NotContain("<script>alert");
        result.HtmlBody.Should().NotContain("</title><h1>Spoofed</h1>");
        result.Subject.Should().Be("Re: Help</title><h1>Spoofed</h1>");
        result.PlainTextBody.Should().Be("<script>alert('xss')</script> you & me");
    }

    [Fact]
    public void RenderEmail_WhenEHelperUsed_ShouldEscapeInHtmlButLeavePlaintextRaw()
    {
        // `e` is the short alias for html_escape; it must behave identically.
        var html = "<html><head><title>t</title></head><body><span>{{ body | e }}</span></body></html>";
        var renderer = CreateRenderer(html, "{{ body | e }}");
        var template = new TestTemplate("alias", "en-US", new { body = "<b>x</b> & y" });

        // Act
        var result = renderer.RenderEmail(template);

        // Assert
        result.HtmlBody.Should().Contain("&lt;b&gt;x&lt;/b&gt; &amp; y");
        result.PlainTextBody.Should().Be("<b>x</b> & y");
    }

    [Fact]
    public void RenderEmail_WhenTitleMissing_ShouldThrow()
    {
        // Arrange
        var renderer = CreateRenderer("<html><body>No title here</body></html>", "No title");

        // Act
        var act = () => renderer.RenderEmail(new TestTemplate("broken", "en-US", new { }));

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*broken*missing*<title>*");
    }

    [Fact]
    public void RenderEmail_WhenTemplateMissing_ShouldThrowFileNotFound()
    {
        // Arrange: real loader pointed at a temp directory with no template files.
        using var tempDir = new TemporaryDirectory();
        var loader = new FileSystemEmailTemplateLoader(tempDir.Path, false);
        var renderer = new ScribanEmailRenderer(_helpers, loader);

        // Act
        var act = () => renderer.RenderEmail(new TestTemplate("missing", "en-US", new { }));

        // Assert
        act.Should().Throw<FileNotFoundException>().WithMessage("*missing.en-US.html*");
    }

    [Fact]
    public void RenderEmail_WhenSubjectHasInternalWhitespace_ShouldCollapseToSingleSpaces()
    {
        // Arrange: title with newlines/tabs inside should produce a clean subject.
        var html = "<html><head><title>\n  Hello\t  {{ name }}  \n</title></head><body></body></html>";
        var renderer = CreateRenderer(html, "");
        var template = new TestTemplate("hello", "en-US", new { name = "Alice" });

        // Act
        var result = renderer.RenderEmail(template);

        // Assert
        result.Subject.Should().Be("Hello Alice");
    }

    [Fact]
    public void RenderEmail_WhenModelHasPascalCaseProperties_ShouldPreserveOriginalNames()
    {
        // Regression guard for the identity renamer in ScribanEmailRenderer. Without it, Scriban's default
        // member renamer rewrites PascalCase property names to snake_case during Import (e.g., OneTimePassword
        // -> one_time_password), silently breaking every production email template that references the model
        // with its original C# name like {{ OneTimePassword }} or {{ TenantName }}.
        var html = "<html><head><title>Code: {{ OneTimePassword }}</title></head><body><p>Tenant: {{ TenantName }}, login at {{ LoginUrl }}</p></body></html>";
        var renderer = CreateRenderer(html, "Code: {{ OneTimePassword }} for {{ TenantName }}");
        var template = new TestTemplate("pascal-case", "en-US", new
            {
                OneTimePassword = "ABC123",
                TenantName = "Acme",
                LoginUrl = "https://example.com/login"
            }
        );

        // Act
        var result = renderer.RenderEmail(template);

        // Assert
        result.Subject.Should().Be("Code: ABC123");
        result.HtmlBody.Should().Contain("Tenant: Acme, login at https://example.com/login");
        result.PlainTextBody.Should().Be("Code: ABC123 for Acme");
    }

    [Fact]
    public void RenderEmail_WhenTemplateReferencesPublicUrl_ShouldUseGlobalFromHelpers()
    {
        // Regression guard for the {{ PublicUrl }} global injected by EmailHelpers.CreateScriptObject.
        // Shared components (e.g., <Footer>) reference this global to construct legal links that resolve
        // to the deploy's host without each handler having to thread PUBLIC_URL through every model.
        var html = "<html><head><title>Footer test</title></head><body><a href=\"{{PublicUrl}}/legal/privacy\">Privacy</a></body></html>";
        var renderer = CreateRenderer(html, "Privacy: {{PublicUrl}}/legal/privacy");
        var template = new TestTemplate("footer-test", "en-US", new { });

        // Act
        var result = renderer.RenderEmail(template);

        // Assert
        result.HtmlBody.Should().Contain("href=\"https://app.platformplatform.net/legal/privacy\"");
        result.PlainTextBody.Should().Be("Privacy: https://app.platformplatform.net/legal/privacy");
    }

    [Fact]
    public void CreateScriptObject_WhenPublicUrlHasTrailingSlash_ShouldTrimIt()
    {
        // Regression guard against accidental double-slashes like https://host//legal/privacy when the
        // PUBLIC_URL env var is configured with a trailing slash.
        var helpers = EmailHelpers.CreateScriptObject("https://app.platformplatform.net/");
        var html = "<html><head><title>x</title></head><body>{{PublicUrl}}/legal/terms</body></html>";
        var renderer = new ScribanEmailRenderer(helpers, new InMemoryEmailTemplateLoader(html, ""));
        var template = new TestTemplate("trim-test", "en-US", new { });

        var result = renderer.RenderEmail(template);

        result.HtmlBody.Should().Contain("https://app.platformplatform.net/legal/terms");
        result.HtmlBody.Should().NotContain("//legal/terms");
    }

    [Fact]
    public void RenderEmail_WhenTemplateReferencesTagline_ShouldUseMailTaglineForTemplateLocale()
    {
        // The mail-channel tagline is locale-specific (each locale can have its own copy), so the
        // renderer pushes {{ Tagline }} as a per-call global rather than baking it into the shared
        // helpers ScriptObject. Verify both supported PP locales resolve to their own mail tagline.
        var html = "<html><head><title>t</title></head><body>{{ Tagline }}</body></html>";
        var renderer = CreateRenderer(html, "{{ Tagline }}");
        var expectedEnglish = Settings.Current.Branding.Tagline.Mail["en-US"];
        var expectedDanish = Settings.Current.Branding.Tagline.Mail["da-DK"];

        var english = renderer.RenderEmail(new TestTemplate("tagline-en", "en-US", new { }));
        var danish = renderer.RenderEmail(new TestTemplate("tagline-da", "da-DK", new { }));

        english.PlainTextBody.Should().Be(expectedEnglish);
        danish.PlainTextBody.Should().Be(expectedDanish);
    }

    [Fact]
    public void RenderEmail_WhenTemplateLocaleHasNoMailTagline_ShouldThrowWithDiagnostic()
    {
        // Guards against silently falling back to en-US for unknown locales -- a missing mail tagline
        // for a locale is a misconfiguration in platform-settings.jsonc and should surface immediately.
        var html = "<html><head><title>t</title></head><body>{{ Tagline }}</body></html>";
        var renderer = CreateRenderer(html, "{{ Tagline }}");

        var act = () => renderer.RenderEmail(new TestTemplate("tagline-missing", "fr-FR", new { }));

        act.Should().Throw<InvalidOperationException>().WithMessage("*branding.tagline.mail*fr-FR*");
    }

    [Fact]
    public void RenderEmail_WhenTemplateHasParseError_ShouldThrowWithTemplateName()
    {
        // Arrange: unterminated for-loop should be caught at parse time.
        var renderer = CreateRenderer("<html><head><title>Bad</title></head><body>{{ for x in items }}no end</body></html>", "");

        // Act
        var act = () => renderer.RenderEmail(new TestTemplate("bad-template", "en-US", new { items = Array.Empty<string>() }));

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("*bad-template*Scriban*");
    }

    private sealed record TestTemplate(string Name, string Locale, object Model) : EmailTemplateBase(Name, Locale, Model);

    private sealed class InMemoryEmailTemplateLoader(string html, string plainText) : IEmailTemplateLoader
    {
        public string LoadHtml(string name, string locale)
        {
            return html;
        }

        public string LoadPlainText(string name, string locale)
        {
            return plainText;
        }
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Directory.CreateDirectory(Path);
        }

        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"EmailRendererTests-{Guid.NewGuid():N}");

        public void Dispose()
        {
            if (Directory.Exists(Path)) Directory.Delete(Path, true);
        }
    }
}
