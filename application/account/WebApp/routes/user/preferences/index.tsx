import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { AppLayout } from "@repo/ui/components/AppLayout";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@repo/ui/components/Select";
import { createFileRoute } from "@tanstack/react-router";
import { MoonIcon, SunIcon } from "lucide-react";

import { OptionCard } from "./-components/OptionCard";
import { ThemeMode, locales, usePreferences } from "./-components/usePreferences";

export const Route = createFileRoute("/user/preferences/")({
  staticData: { trackingTitle: "User preferences" },
  component: PreferencesPage
});

function PreferencesPage() {
  const {
    theme,
    currentLocale,
    currentZoomLevel,
    zoomLevelOptions,
    handleLocaleChange,
    handleZoomLevelChange,
    handleThemeChange,
    getSystemThemeIcon
  } = usePreferences();

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
          <p className="mb-4 text-sm text-muted-foreground">
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
          <p className="mb-4 text-sm text-muted-foreground">
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
          <p className="mb-4 text-sm text-muted-foreground">
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
