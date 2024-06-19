import { createContext } from "react";
import type { Locale, LocaleInfo } from "./Translation";
export type { Locale, LocaleInfo } from "./Translation";

export type SetLocalFunction = (locale: Locale) => Promise<void>;

export type TranslationContext = {
  setLocale: SetLocalFunction;
  locales: Locale[];
  getLocaleInfo(locale: Locale | "pseudo"): LocaleInfo;
};

export const translationContext = createContext<TranslationContext>({
  setLocale: async () => {},
  locales: [],
  getLocaleInfo: () => {
    throw new Error("Not initialized");
  }
});
