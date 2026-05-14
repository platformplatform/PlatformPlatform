import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { AppLayout } from "@repo/ui/components/AppLayout";
import { ToggleGroup, ToggleGroupItem } from "@repo/ui/components/ToggleGroup";
import { createFileRoute } from "@tanstack/react-router";
import { MoonIcon, SunIcon } from "lucide-react";

import { PreferencesFeatureFlagsSection } from "./-components/PreferencesFeatureFlagsSection";
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
          <ToggleGroup
            variant="outline"
            size="lg"
            className="flex w-full"
            value={[theme ?? ThemeMode.System]}
            onValueChange={(values) => {
              if (values.length > 0) {
                handleThemeChange(values[0]);
              }
            }}
          >
            <ToggleGroupItem className="flex-1" value={ThemeMode.System}>
              {getSystemThemeIcon()}
              <Trans>System</Trans>
            </ToggleGroupItem>
            <ToggleGroupItem className="flex-1" value={ThemeMode.Light}>
              <SunIcon className="size-5" />
              <Trans>Light</Trans>
            </ToggleGroupItem>
            <ToggleGroupItem className="flex-1" value={ThemeMode.Dark}>
              <MoonIcon className="size-5" />
              <Trans>Dark</Trans>
            </ToggleGroupItem>
          </ToggleGroup>
        </section>

        <section>
          <h3 className="mb-1">
            <Trans>Language</Trans>
          </h3>
          <p className="mb-4 text-sm text-muted-foreground">
            <Trans>Select your preferred language. Saved to your profile.</Trans>
          </p>
          <ToggleGroup
            variant="outline"
            size="lg"
            className="flex w-full"
            value={[currentLocale]}
            onValueChange={(values) => {
              if (values.length > 0) {
                handleLocaleChange(values[0] as (typeof locales)[number]["id"]);
              }
            }}
          >
            {locales.map((locale) => (
              <ToggleGroupItem key={locale.id} className="flex-1" value={locale.id}>
                <span className="text-lg leading-none">{locale.flag}</span>
                {locale.label}
              </ToggleGroupItem>
            ))}
          </ToggleGroup>
        </section>

        <section>
          <h3 className="mb-1">
            <Trans>Zoom</Trans>
          </h3>
          <p className="mb-4 text-sm text-muted-foreground">
            <Trans>Adjust the interface size on this device to your preference.</Trans>
          </p>
          <ToggleGroup
            variant="outline"
            size="lg"
            className="flex w-full"
            value={[currentZoomLevel]}
            onValueChange={(values) => {
              if (values.length > 0) {
                handleZoomLevelChange(values[0]);
              }
            }}
          >
            {zoomLevelOptions.map((zoom) => (
              <ToggleGroupItem key={zoom.value} className="flex-1" value={zoom.value}>
                <span style={{ fontSize: zoom.fontSize, lineHeight: 1 }}>Aa</span>
                {zoom.label}
              </ToggleGroupItem>
            ))}
          </ToggleGroup>
        </section>

        <PreferencesFeatureFlagsSection />
      </div>
    </AppLayout>
  );
}
