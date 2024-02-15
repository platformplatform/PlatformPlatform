import Zod from "zod";
import type { I18n, Messages } from "@lingui/core";
import i18nConfig from "./i18n.config.json";

export const pseudoLocale = "pseudo";
export const LocaleSchema = Zod.enum(["da-DK", "en-US", pseudoLocale]);
export const sourceLocale = LocaleSchema.parse(i18nConfig.defaultLocale);

export type Language = (typeof i18nConfig.languages)["en-US"];
export type Locale = keyof typeof i18nConfig.languages | typeof pseudoLocale;
export const locales = [...Object.keys(i18nConfig.languages)] as Array<Locale>;

// eslint-disable-next-line node/prefer-global/process
if (process.env.NODE_ENV !== "production")
  locales.push(pseudoLocale);

export function getLanguage(locale: Locale): Language {
  if (locale === pseudoLocale) {
    return {
      label: "Pseudo",
      locale: pseudoLocale,
      territory: pseudoLocale,
      rtl: false,
    };
  }
  return i18nConfig.languages[locale];
}

export function getInitialLocale(): Locale {
  const result = LocaleSchema.safeParse(import.meta.env.LOCALE ?? sourceLocale);
  return result.success ? result.data : sourceLocale;
}

const messageCache = new Map<string, Messages>();

export async function loadCatalog(locale: string) {
  if (messageCache.has(locale) === false) {
    const { messages } = (await import(`@/translations/locale/${locale}.ts`)) as {
      messages: Messages,
    };
    messageCache.set(locale, messages);
  }

  return messageCache.get(locale) as Messages;
}

export async function dynamicActivate(i18n: I18n, locale: string) {
  const messages = await loadCatalog(locale);
  i18n.loadAndActivate({ locale, messages });
}
