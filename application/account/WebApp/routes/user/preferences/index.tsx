import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { preferredLocaleKey } from "@repo/infrastructure/translations/constants";
import localeMap from "@repo/infrastructure/translations/i18n.config.json";
import { type Locale, translationContext } from "@repo/infrastructure/translations/TranslationContext";
import { AppLayout } from "@repo/ui/components/AppLayout";
import { Button } from "@repo/ui/components/Button";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@repo/ui/components/Select";
import { createFileRoute } from "@tanstack/react-router";
import { CheckIcon, MoonIcon, MoonStarIcon, SunIcon, SunMoonIcon } from "lucide-react";
import { useTheme } from "next-themes";
import { use, useEffect, useState } from "react";
import { api } from "@/shared/lib/api/client";

const zoomLevelStorageKey = "zoom-level";

const defaultZoomValue = "1";

const localeFlagMap: Record<string, string> = {
  "en-US": "\u{1F1FA}\u{1F1F8}",
  "da-DK": "\u{1F1E9}\u{1F1F0}"
};

const locales = Object.entries(localeMap).map(([id, info]) => ({
  id: id as Locale,
  label: info.label,
  flag: localeFlagMap[id] ?? ""
}));

const ThemeMode = {
  System: "system",
  Light: "light",
  Dark: "dark"
} as const;

export const Route = createFileRoute("/user/preferences/")({
  component: PreferencesPage
});

function OptionCard({
  icon,
  label,
  isSelected,
  onClick
}: Readonly<{
  icon: React.ReactNode;
  label: string;
  isSelected: boolean;
  onClick: () => void;
}>) {
  return (
    <Button
      variant="outline"
      aria-pressed={isSelected}
      onClick={onClick}
      className={`h-auto w-full justify-start gap-3 rounded-lg p-4 font-normal ${
        isSelected ? "border-primary bg-primary/5 hover:bg-primary/5 active:bg-primary/10" : "active:bg-accent"
      }`}
    >
      <span className="text-muted-foreground">{icon}</span>
      <span className="flex-1 text-left text-sm">{label}</span>
      {isSelected && <CheckIcon className="size-4 text-primary" />}
    </Button>
  );
}

function PreferencesPage() {
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
    if (savedZoomLevel && zoomLevelOptions.some((z) => z.value === savedZoomLevel)) {
      setCurrentZoomLevel(savedZoomLevel);
    }
  }, []);

  const handleLocaleChange = (locale: Locale) => {
    if (locale === currentLocale) {
      return;
    }

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

  return (
    <AppLayout
      variant="center"
      maxWidth="64rem"
      balanceWidth="16rem"
      title={t`User preferences`}
      subtitle={t`Customize your theme, language, and zoom settings.`}
    >
      <div className="flex flex-col gap-8 pt-8">
        <section>
          <h3 className="mb-1">
            <Trans>Theme</Trans>
          </h3>
          <p className="mb-4 text-muted-foreground text-sm">
            <Trans>Choose how the application looks to you on this device.</Trans>
          </p>
          <div className="grid gap-3 sm:grid-cols-3">
            <OptionCard
              icon={getSystemThemeIcon()}
              label={t`System`}
              isSelected={theme === ThemeMode.System}
              onClick={() => handleThemeChange(ThemeMode.System)}
            />
            <OptionCard
              icon={<SunIcon className="size-5" />}
              label={t`Light`}
              isSelected={theme === ThemeMode.Light}
              onClick={() => handleThemeChange(ThemeMode.Light)}
            />
            <OptionCard
              icon={<MoonIcon className="size-5" />}
              label={t`Dark`}
              isSelected={theme === ThemeMode.Dark}
              onClick={() => handleThemeChange(ThemeMode.Dark)}
            />
          </div>
        </section>

        <section>
          <h3 className="mb-1">
            <Trans>Language</Trans>
          </h3>
          <p className="mb-4 text-muted-foreground text-sm">
            <Trans>Select your preferred language. Saved to your profile.</Trans>
          </p>
          <div className="grid gap-3 sm:grid-cols-2">
            {locales.map((locale) => (
              <OptionCard
                key={locale.id}
                icon={<span className="text-lg leading-none">{locale.flag}</span>}
                label={locale.label}
                isSelected={locale.id === currentLocale}
                onClick={() => handleLocaleChange(locale.id)}
              />
            ))}
          </div>
        </section>

        <section>
          <h3 className="mb-1">
            <Trans>Zoom</Trans>
          </h3>
          <p className="mb-4 text-muted-foreground text-sm">
            <Trans>Adjust the interface size on this device to your preference.</Trans>
          </p>
          <Select value={currentZoomLevel} onValueChange={handleZoomLevelChange}>
            <SelectTrigger className="w-full" aria-label={t`Zoom level`}>
              <SelectValue>{() => zoomLevelOptions.find((z) => z.value === currentZoomLevel)?.label}</SelectValue>
            </SelectTrigger>
            <SelectContent>
              {zoomLevelOptions.map((zoom) => (
                <SelectItem key={zoom.value} value={zoom.value}>
                  <span className="flex items-center gap-2">
                    <span style={{ fontSize: zoom.fontSize, lineHeight: 1 }}>Aa</span>
                    {zoom.label}
                  </span>
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </section>
      </div>
    </AppLayout>
  );
}
