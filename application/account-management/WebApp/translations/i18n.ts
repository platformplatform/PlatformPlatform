import Zod from "zod";
import i18nConfig from "./i18n.config.json";
import { rtlLocale } from "./lib/direction";

export const LANGUAGE_COOKIE = "USER_LOCALE";
export const pseudoLocale = "pseudo";

export type Language = (typeof i18nConfig.languages)["en-US"];
export type Locale = keyof typeof i18nConfig.languages | typeof pseudoLocale;
export const locales = [...Object.keys(i18nConfig.languages)] as Array<Locale>;

if (process.env.NODE_ENV !== "production") {
  locales.push(pseudoLocale);
}

export function getLanguage(locale: Locale): Language {
  if (locale === pseudoLocale) {
    return {
      label: "Pseudo",
      locale: pseudoLocale,
    };
  }
  return i18nConfig.languages[locale];
}

export const LocaleSchema = Zod.enum(["da-DK", "en-US", "pseudo"]);
export const defaultLocale: Locale = LocaleSchema.parse("en-US");

export function parseLocale(locale?: string | null): Locale | null {
  const result = LocaleSchema.safeParse(locale);
  if (result.success) return result.data;
  return null;
}

export function getLocaleDirection(locale: Locale): "ltr" | "rtl" {
  return rtlLocale.includes(locale) ? "rtl" : "ltr";
}
