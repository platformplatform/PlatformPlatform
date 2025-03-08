import { type Messages, i18n } from "@lingui/core";
import { I18nProvider } from "@lingui/react";
import type React from "react";
import { useMemo, useState } from "react";
import { type TranslationContext, translationContext } from "./TranslationContext";
import localeMap from "./i18n.config.json";

export type Locale = keyof typeof localeMap;

export type LocaleInfo = {
  label: string;
  locale: string;
  territory: string;
  rtl: boolean;
};

export type LocaleMap = Record<Locale, LocaleInfo>;

export type LocaleFile = {
  messages: Messages;
};

export type LocalLoaderFunction = (locale: Locale) => Promise<LocaleFile>;

const TranslationContextProvider = translationContext.Provider;

export class Translation {
  private readonly _messageCache = new Map<Locale, LocaleFile>();
  private readonly _defaultLocale = document.documentElement.lang as Locale;

  /**
   * Prefer using `TranslationConfig.create` instead of this constructor
   */
  private readonly localeLoader: LocalLoaderFunction;

  constructor(localeLoader: LocalLoaderFunction) {
    this.localeLoader = localeLoader;
  }

  private readonly _locales: Locale[] = Object.keys(localeMap) as Locale[];

  /**
   * Get the list of available locales
   */
  public get locales(): Locale[] {
    return this._locales;
  }

  /**
   * Create a new TranslationConfig instance and load the initial locale
   */
  public static async create(localeLoader: LocalLoaderFunction): Promise<Translation> {
    const config = new Translation(localeLoader);
    await config.dynamicActivate();
    return config;
  }

  /**
   * Load and activate the given locale
   */
  public async dynamicActivate(newLocale?: string | undefined) {
    const locale = this.getLocale(newLocale);
    const { messages } = await this.loadCatalog(locale);
    i18n.loadAndActivate({ locale: locale as string, messages });
  }

  /**
   * Get the locale info for the given locale
   */
  public getLocaleInfo(locale: Locale): LocaleInfo {
    return localeMap[locale];
  }

  /**
   * This component should be used as a wrapper around the application to provide
   * the translation context to the rest of the application
   *
   * @param children The children to render
   */
  // biome-ignore lint/style/useNamingConvention: React components use PascalCase by convention
  public TranslationProvider = ({ children }: { children: React.ReactNode }) => {
    return <TranslationProvider translation={this}>{children}</TranslationProvider>;
  };

  /**
   * Get the locale for the application
   */
  private getLocale(locale?: string): Locale {
    if (locale && this.isLocale(locale)) {
      return locale;
    }
    if (import.meta.env.LOCALE && this.isLocale(import.meta.env.LOCALE)) {
      return import.meta.env.LOCALE;
    }
    return this._defaultLocale;
  }

  /**
   * Load the catalog for the given locale
   */
  private async loadCatalog(locale: Locale): Promise<LocaleFile> {
    const existingLocaleFile = this._messageCache.get(locale);
    if (existingLocaleFile) {
      return existingLocaleFile;
    }

    const messageFile = await this.localeLoader(locale);
    this._messageCache.set(locale, messageFile);

    return messageFile;
  }

  /**
   * Assert that the given string is a valid locale
   */
  private isLocale(locale: string): locale is Locale {
    return locale in localeMap;
  }
}

type TranslationProviderProps = {
  translation: Translation;
  children: React.ReactNode;
};

function TranslationProvider({ children, translation }: Readonly<TranslationProviderProps>) {
  const [currentLocale, setCurrentLocale] = useState(i18n.locale);

  const value: TranslationContext = useMemo(
    () => ({
      setLocale: async (locale: string) => {
        await translation.dynamicActivate(locale);
        setCurrentLocale(locale); // Update state to force re-render
      },
      locales: translation.locales,
      getLocaleInfo: translation.getLocaleInfo
    }),
    [translation]
  );

  return (
    <TranslationContextProvider value={value}>
      <I18nProvider key={currentLocale} i18n={i18n}>
        {children}
      </I18nProvider>
    </TranslationContextProvider>
  );
}
