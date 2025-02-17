import { useContext, useEffect } from "react";
import { AuthenticationContext } from "../auth/AuthenticationProvider";
import type { Locale } from "./Translation";
import { translationContext } from "./TranslationContext";
import { preferredLocaleKey } from "./constants";

export function useInitializeLocale() {
  const { userInfo } = useContext(AuthenticationContext);
  const { setLocale } = useContext(translationContext);

  useEffect(() => {
    if (userInfo?.isAuthenticated) {
      localStorage.setItem(preferredLocaleKey, document.documentElement.lang);
    } else {
      const storedLocale = localStorage.getItem(preferredLocaleKey) as Locale;
      if (storedLocale) {
        document.documentElement.lang = storedLocale;
        setLocale(storedLocale);
      }
    }
  }, [userInfo?.isAuthenticated, setLocale]);
}
