import { I18nProvider } from "@lingui/react";
import { dynamicActivate, getInitialLocale } from "./i18n";
import { useEffect } from "react";
import { i18n } from "@lingui/core";

export function TranslationProvider({ children }: { children: React.ReactNode }) {
  useEffect(() => {
    dynamicActivate(i18n, getInitialLocale());
  }, []);
  return <I18nProvider i18n={i18n}>{children}</I18nProvider>;
}
