import { useEffect, useContext } from "react";
import { preferredLocaleKey } from "./constants";
import { AuthenticationContext } from "../auth/AuthenticationProvider";
import { translationContext } from "./TranslationContext";
import type { Locale } from "./Translation";

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
