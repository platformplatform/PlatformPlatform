using System.Globalization;
using System.Net;
using Humanizer;
using Scriban.Runtime;
using SharedKernel.Platform;

namespace SharedKernel.Emails;

// Static helpers exposed to Scriban templates via ScriptObject.Import. Method names map to snake_case
// in templates (e.g., FormatCurrency → format_currency). Templates invoke them with pipe syntax:
//
//   {{ amount | format_currency "USD" "en-US" }}
//   {{ date | format_date "en-US" }}
//   {{ count | pluralize "item" }}
//
// Scriban performs parameter binding and type coercion based on the C# signatures below — no manual
// argument unpacking is needed (in contrast to Handlebars.Net's untyped Arguments dictionary).
//
// Templates can also reference globals exposed alongside the helpers — `{{ PublicUrl }}` (the
// trimmed PUBLIC_URL of the running deploy used to construct environment-correct links),
// `{{ ProductName }}` (brand name), and `{{ EmailHeaderBackground }}` (CSS color shown behind the
// transparent email-header banner). `{{ Tagline }}` (one-line product description shown in the
// email footer) is also a global, but it is locale-dependent and is therefore pushed onto the
// per-render Scriban context by the renderer (see ScribanEmailRenderer). The brand strings come
// from platform-settings.jsonc.
internal static class EmailHelpers
{
    public static ScriptObject CreateScriptObject(string publicUrl)
    {
        var scriptObject = new ScriptObject();
        scriptObject.Import("format_currency", FormatCurrency);
        scriptObject.Import("format_date", FormatDate);
        scriptObject.Import("pluralize", Pluralize);
        // HTML-escape untrusted text interpolated into the HTML body. Scriban does no auto-escaping,
        // so any template that substitutes user- or staff-controlled text MUST pipe it through this
        // (e.g. {{ Body | html_escape }}). `e` is the conventional short alias.
        scriptObject.Import("html_escape", HtmlEscape);
        scriptObject.Import("e", HtmlEscape);
        scriptObject.SetValue("PublicUrl", publicUrl.TrimEnd('/'), true);
        scriptObject.SetValue("ProductName", Settings.Current.Branding.ProductName, true);
        scriptObject.SetValue("EmailHeaderBackground", Settings.Current.Branding.EmailHeaderBackground, true);
        return scriptObject;
    }

    private static string HtmlEscape(string? value)
    {
        return WebUtility.HtmlEncode(value ?? "");
    }

    private static string FormatCurrency(decimal amount, string currency, string locale)
    {
        var culture = CultureInfo.GetCultureInfo(locale);
        var numberFormat = (NumberFormatInfo)culture.NumberFormat.Clone();
        numberFormat.CurrencySymbol = ResolveCurrencySymbol(currency, culture);
        return amount.ToString("C", numberFormat);
    }

    private static string FormatDate(DateTimeOffset value, string locale, string? format = null)
    {
        var culture = CultureInfo.GetCultureInfo(locale);
        return format is null ? value.ToString("D", culture) : value.ToString(format, culture);
    }

    private static string Pluralize(int count, string singular, string? plural = null)
    {
        return count == 1 ? singular : plural ?? singular.Pluralize(false);
    }

    private static string ResolveCurrencySymbol(string currencyCode, CultureInfo culture)
    {
        // Match the symbol shown by RegionInfo when the culture's currency matches; otherwise fall back to
        // the ISO code so EUR formatted with en-US shows as "EUR 12.34" instead of mis-labelling as "$".
        try
        {
            var region = new RegionInfo(culture.Name);
            if (string.Equals(region.ISOCurrencySymbol, currencyCode, StringComparison.OrdinalIgnoreCase))
            {
                return region.CurrencySymbol;
            }
        }
        catch (ArgumentException)
        {
            // Neutral or invalid culture; fall through.
        }

        return currencyCode.ToUpperInvariant();
    }
}
