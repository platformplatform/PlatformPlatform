import { useMemo } from "react";
import { I18nProvider } from "@lingui/react";
import { i18n, type Messages } from "@lingui/core";
import localeMap from "./i18n.config.json";
import { translationContext, type TranslationContext } from "./TranslationContext";

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
  private _messageCache = new Map<Locale, LocaleFile>();
  private _defaultLocale: Locale = "en-US";
  private _locales: Locale[] = Object.keys(localeMap) as Locale[];

  /**
   * Create a new TranslationConfig instance and load the initial locale
   */
  public static async create(localeLoader: LocalLoaderFunction): Promise<Translation> {
    const config = new Translation(localeLoader);
    await config.dynamicActivate();
    return config;
  }

  /**
   * Prefer using `TranslationConfig.create` instead of this constructor
   */
  constructor(private localeLoader: LocalLoaderFunction) {}

  /**
   * Get the list of available locales
   */
  public get locales(): Locale[] {
    return this._locales;
  }

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
  public getLocaleInfo(locale: Locale | "pseudo"): LocaleInfo {
    if (locale === "pseudo") {
      return {
        label: "Pseudo",
        locale: "pseudo",
        territory: "pseudo",
        rtl: false
      };
    }
    return localeMap[locale];
  }

  /**
   * Assert that the given string is a valid locale
   */
  private isLocale(locale: string): locale is Locale {
    return locale in localeMap;
  }

  /**
   * This component should be used as a wrapper around the application to provide
   * the translation context to the rest of the application
   *
   * @param children The children to render
   */
  public TranslationProvider = ({ children }: { children: React.ReactNode }) => {
    return <TranslationProvider translation={this}>{children}</TranslationProvider>;
  };
}

type TranslationProviderProps = {
  translation: Translation;
  children: React.ReactNode;
};

function TranslationProvider({ children, translation }: Readonly<TranslationProviderProps>) {
  const value: TranslationContext = useMemo(
    () => ({
      setLocale: async (locale: string) => {
        await translation.dynamicActivate(locale);
      },
      locales: translation.locales,
      getLocaleInfo: translation.getLocaleInfo
    }),
    [translation]
  );
  return (
    <TranslationContextProvider value={value}>
      <I18nProvider i18n={i18n}>{children}</I18nProvider>
    </TranslationContextProvider>
  );
}
