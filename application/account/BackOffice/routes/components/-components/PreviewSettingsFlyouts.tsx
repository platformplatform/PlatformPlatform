import { t } from "@lingui/core/macro";
import { useLingui } from "@lingui/react";
import { Trans } from "@lingui/react/macro";
import { preferredLocaleKey } from "@repo/infrastructure/translations/constants";
import { type Locale, translationContext } from "@repo/infrastructure/translations/TranslationContext";
import { Button } from "@repo/ui/components/Button";
import { Popover, PopoverContent, PopoverTrigger } from "@repo/ui/components/Popover";
import { SidebarMenuButton } from "@repo/ui/components/Sidebar";
import {
  CheckIcon,
  ChevronRightIcon,
  GlobeIcon,
  MoonIcon,
  MoonStarIcon,
  SunIcon,
  SunMoonIcon,
  ZoomInIcon
} from "lucide-react";
import { useTheme } from "next-themes";
import { use, useEffect, useState } from "react";

const zoomLevelStorageKey = "zoom-level";

const zoomLevelOptions = [
  { value: "0.875", label: () => t`Small` },
  { value: "1", label: () => t`Default` },
  { value: "1.125", label: () => t`Large` },
  { value: "1.25", label: () => t`Larger` }
];

export function PreviewThemeFlyout() {
  const { theme, setTheme, resolvedTheme } = useTheme();
  const icon =
    theme === "dark" ? (
      <MoonIcon />
    ) : theme === "light" ? (
      <SunIcon />
    ) : resolvedTheme === "dark" ? (
      <MoonStarIcon />
    ) : (
      <SunMoonIcon />
    );
  return (
    <Popover>
      <PopoverTrigger
        render={
          <SidebarMenuButton aria-label={t`Change theme`}>
            {icon}
            <span>
              <Trans>Theme</Trans>
            </span>
            <ChevronRightIcon className="ml-auto" />
          </SidebarMenuButton>
        }
      />
      <PopoverContent side="right" align="start" className="w-auto min-w-[12rem] p-1">
        <Button variant="ghost" onClick={() => setTheme("system")} className="w-full justify-start gap-2">
          {resolvedTheme === "dark" ? <MoonStarIcon className="size-5" /> : <SunMoonIcon className="size-5" />}
          <Trans>System</Trans>
          {theme === "system" && <CheckIcon className="ml-auto size-4" />}
        </Button>
        <Button variant="ghost" onClick={() => setTheme("light")} className="w-full justify-start gap-2">
          <SunIcon className="size-5" />
          <Trans>Light</Trans>
          {theme === "light" && <CheckIcon className="ml-auto size-4" />}
        </Button>
        <Button variant="ghost" onClick={() => setTheme("dark")} className="w-full justify-start gap-2">
          <MoonIcon className="size-5" />
          <Trans>Dark</Trans>
          {theme === "dark" && <CheckIcon className="ml-auto size-4" />}
        </Button>
      </PopoverContent>
    </Popover>
  );
}

export function PreviewLanguageFlyout() {
  const { setLocale, getLocaleInfo, locales } = use(translationContext);
  const { i18n } = useLingui();
  const currentLocale = i18n.locale as Locale;
  const handleLocaleChange = (locale: Locale) => {
    if (locale !== currentLocale) {
      setLocale(locale).then(() => {
        localStorage.setItem(preferredLocaleKey, locale);
      });
    }
  };
  return (
    <Popover>
      <PopoverTrigger
        render={
          <SidebarMenuButton aria-label={t`Change language`}>
            <GlobeIcon />
            <span>
              <Trans>Language</Trans>
            </span>
            <ChevronRightIcon className="ml-auto" />
          </SidebarMenuButton>
        }
      />
      <PopoverContent side="right" align="start" className="w-auto min-w-[12rem] p-1">
        {locales.map((locale) => (
          <Button
            key={locale}
            variant="ghost"
            onClick={() => handleLocaleChange(locale)}
            className="w-full justify-start"
          >
            <span>{getLocaleInfo(locale).label}</span>
            {locale === currentLocale && <CheckIcon className="ml-auto size-4" />}
          </Button>
        ))}
      </PopoverContent>
    </Popover>
  );
}

export function PreviewZoomFlyout() {
  const [currentZoomLevel, setCurrentZoomLevel] = useState("1");
  useEffect(() => {
    const saved = localStorage.getItem(zoomLevelStorageKey);
    if (saved) setCurrentZoomLevel(saved);
  }, []);
  const handleZoomChange = (value: string) => {
    if (value === currentZoomLevel) return;
    if (value === "1") {
      localStorage.removeItem(zoomLevelStorageKey);
    } else {
      localStorage.setItem(zoomLevelStorageKey, value);
    }
    document.documentElement.style.setProperty("--zoom-level", value);
    setCurrentZoomLevel(value);
    window.location.reload();
  };
  return (
    <Popover>
      <PopoverTrigger
        render={
          <SidebarMenuButton aria-label={t`Change zoom level`}>
            <ZoomInIcon />
            <span>
              <Trans>Zoom</Trans>
            </span>
            <ChevronRightIcon className="ml-auto" />
          </SidebarMenuButton>
        }
      />
      <PopoverContent side="right" align="start" className="w-auto min-w-[12rem] p-1">
        {zoomLevelOptions.map((zoom) => (
          <Button
            key={zoom.value}
            variant="ghost"
            onClick={() => handleZoomChange(zoom.value)}
            className="w-full justify-start"
          >
            <span>{zoom.label()}</span>
            {zoom.value === currentZoomLevel && <CheckIcon className="ml-auto size-4" />}
          </Button>
        ))}
      </PopoverContent>
    </Popover>
  );
}
