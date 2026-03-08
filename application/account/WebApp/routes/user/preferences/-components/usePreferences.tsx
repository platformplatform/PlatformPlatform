import { t } from "@lingui/core/macro";
import { trackInteraction } from "@repo/infrastructure/applicationInsights/ApplicationInsightsProvider";
import { preferredLocaleKey } from "@repo/infrastructure/translations/constants";
import localeMap from "@repo/infrastructure/translations/i18n.config.json";
import { type Locale, translationContext } from "@repo/infrastructure/translations/TranslationContext";
import { MoonStarIcon, SunMoonIcon } from "lucide-react";
import { useTheme } from "next-themes";
import { use, useEffect, useState } from "react";

import { api } from "@/shared/lib/api/client";

const zoomLevelStorageKey = "zoom-level";

const defaultZoomValue = "1";

const validZoomValues = ["0.875", "1", "1.125", "1.25"];

const localeFlagMap: Record<string, string> = {
  "en-US": "\u{1F1FA}\u{1F1F8}",
  "da-DK": "\u{1F1E9}\u{1F1F0}"
};

export const locales = Object.entries(localeMap).map(([id, info]) => ({
  id: id as Locale,
  label: info.label,
  flag: localeFlagMap[id] ?? ""
}));

export const ThemeMode = {
  System: "system",
  Light: "light",
  Dark: "dark"
} as const;

export function usePreferences() {
  const { theme, setTheme, resolvedTheme } = useTheme();
  const [currentLocale, setCurrentLocale] = useState<Locale>("en-US");
  const [currentZoomLevel, setCurrentZoomLevel] = useState(defaultZoomValue);
  const { setLocale } = use(translationContext);

  const zoomLevelOptions = [
    { value: "0.875", label: t`Small`, fontSize: "14px" },
    { value: "1", label: t`Default`, fontSize: "16px" },
    { value: "1.125", label: t`Large`, fontSize: "18px" },
    { value: "1.25", label: t`Larger`, fontSize: "20px" }
  ];

  const changeLocaleMutation = api.useMutation("put", "/api/account/users/me/change-locale");
  const changeZoomLevelMutation = api.useMutation("put", "/api/account/users/me/change-zoom-level", {
    meta: { skipQueryInvalidation: true }
  });
  const changeThemeMutation = api.useMutation("put", "/api/account/users/me/change-theme", {
    meta: { skipQueryInvalidation: true }
  });

  useEffect(() => {
    const htmlLang = document.documentElement.lang as Locale;
    const savedLocale = localStorage.getItem(preferredLocaleKey) as Locale;

    if (savedLocale && locales.some((l) => l.id === savedLocale)) {
      setCurrentLocale(savedLocale);
    } else if (htmlLang && locales.some((l) => l.id === htmlLang)) {
      setCurrentLocale(htmlLang);
    }

    const savedZoomLevel = localStorage.getItem(zoomLevelStorageKey);
    if (savedZoomLevel && validZoomValues.includes(savedZoomLevel)) {
      setCurrentZoomLevel(savedZoomLevel);
    }
  }, []);

  const handleLocaleChange = (locale: Locale) => {
    if (locale === currentLocale) {
      return;
    }

    const localeLabel = locales.find((l) => l.id === locale)?.label ?? locale;
    trackInteraction("User preferences", "interaction", `Change language to "${localeLabel}"`);
    localStorage.setItem(preferredLocaleKey, locale);
    changeLocaleMutation.mutate(
      { body: { locale } },
      {
        onSuccess: async () => {
          document.documentElement.lang = locale;
          await setLocale(locale);
          setCurrentLocale(locale);
        }
      }
    );
  };

  const handleZoomLevelChange = (value: string | null) => {
    if (!value || value === currentZoomLevel) {
      return;
    }

    const zoomLabelMap: Record<string, string> = {
      "0.875": "Small",
      "1": "Default",
      "1.125": "Large",
      "1.25": "Larger"
    };
    const zoomLabel = zoomLabelMap[value] ?? value;
    trackInteraction("User preferences", "interaction", `Change zoom to "${zoomLabel}"`);
    changeZoomLevelMutation.mutate({ body: { fromZoomLevel: currentZoomLevel, zoomLevel: value } });

    if (value === "1") {
      localStorage.removeItem(zoomLevelStorageKey);
    } else {
      localStorage.setItem(zoomLevelStorageKey, value);
    }
    document.documentElement.style.setProperty("--zoom-level", value);
    setCurrentZoomLevel(value);
  };

  const handleThemeChange = (newTheme: string) => {
    let themeLabel = "Light";
    if (newTheme === "system") {
      const systemIsDark = window.matchMedia("(prefers-color-scheme: dark)").matches;
      themeLabel = `System (${systemIsDark ? "Dark" : "Light"})`;
    } else if (newTheme === "dark") {
      themeLabel = "Dark";
    }
    trackInteraction("User preferences", "interaction", `Change theme to "${themeLabel}"`);
    changeThemeMutation.mutate({
      body: {
        fromTheme: theme ?? "system",
        theme: newTheme,
        resolvedTheme: newTheme === "system" ? (resolvedTheme ?? null) : null
      }
    });
    setTheme(newTheme);
  };

  const getSystemThemeIcon = () => {
    if (resolvedTheme === ThemeMode.Dark) {
      return <MoonStarIcon className="size-5" />;
    }
    return <SunMoonIcon className="size-5" />;
  };

  return {
    theme,
    currentLocale,
    currentZoomLevel,
    zoomLevelOptions,
    handleLocaleChange,
    handleZoomLevelChange,
    handleThemeChange,
    getSystemThemeIcon
  };
}
