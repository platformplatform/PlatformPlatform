import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import type { Key } from "@react-types/shared";
import { Button } from "@repo/ui/components/Button";
import { Menu, MenuItem, MenuTrigger } from "@repo/ui/components/Menu";
import { Tooltip, TooltipTrigger } from "@repo/ui/components/Tooltip";
import { CheckIcon, MoonIcon, MoonStarIcon, SunIcon, SunMoonIcon } from "lucide-react";
import { useEffect, useState } from "react";

const THEME_MODE_KEY = "theme-mode";

enum ThemeMode {
  System = "system",
  Light = "light",
  Dark = "dark"
}

export default function ThemeModeSelector() {
  const [themeMode, setThemeModeState] = useState<ThemeMode>(ThemeMode.System);

  useEffect(() => {
    // Read initial theme mode from localStorage
    const savedMode = localStorage.getItem(THEME_MODE_KEY) as ThemeMode;
    const initialMode = savedMode && Object.values(ThemeMode).includes(savedMode) ? savedMode : ThemeMode.System;
    setThemeModeState(initialMode);
    
    // Apply initial theme
    const root = document.documentElement;
    root.classList.remove("light", "dark");
    
    if (initialMode === ThemeMode.Dark) {
      root.classList.add("dark");
      root.style.colorScheme = "dark";
    } else if (initialMode === ThemeMode.Light) {
      root.classList.add("light");
      root.style.colorScheme = "light";
    } else {
      // System mode - check system preference
      const prefersDark = window.matchMedia("(prefers-color-scheme: dark)").matches;
      if (prefersDark) {
        root.classList.add("dark");
        root.style.colorScheme = "dark";
      } else {
        root.classList.add("light");
        root.style.colorScheme = "light";
      }
    }
  }, []);

  const handleThemeChange = (key: Key) => {
    const newMode = key as ThemeMode;
    setThemeModeState(newMode);
    localStorage.setItem(THEME_MODE_KEY, newMode);
    
    // Apply theme to DOM - matching the original implementation
    const root = document.documentElement;
    
    // Remove both light and dark classes first
    root.classList.remove("light", "dark");
    
    if (newMode === ThemeMode.Dark) {
      root.classList.add("dark");
      root.style.colorScheme = "dark";
    } else if (newMode === ThemeMode.Light) {
      root.classList.add("light");
      root.style.colorScheme = "light";
    } else {
      // System mode - check system preference
      const prefersDark = window.matchMedia("(prefers-color-scheme: dark)").matches;
      if (prefersDark) {
        root.classList.add("dark");
        root.style.colorScheme = "dark";
      } else {
        root.classList.add("light");
        root.style.colorScheme = "light";
      }
    }
    
    // Dispatch event to notify other components
    window.dispatchEvent(new CustomEvent("theme-mode-changed", { detail: newMode }));
  };

  const getThemeIcon = () => {
    switch (themeMode) {
      case ThemeMode.Dark:
        return <MoonIcon className="h-5 w-5" />;
      case ThemeMode.Light:
        return <SunIcon className="h-5 w-5" />;
      default:
        // For system mode, show icon based on actual system preference
        const prefersDark = window.matchMedia("(prefers-color-scheme: dark)").matches;
        return prefersDark ? <MoonStarIcon className="h-5 w-5" /> : <SunMoonIcon className="h-5 w-5" />;
    }
  };

  const menuContent = (
    <MenuTrigger>
      <Button variant="icon" aria-label={t`Change theme`}>
        {getThemeIcon()}
      </Button>
      <Menu onAction={handleThemeChange} aria-label={t`Change theme`} placement="bottom end">
        <MenuItem id={ThemeMode.System} textValue="System">
          <div className="flex items-center gap-2">
            {window.matchMedia("(prefers-color-scheme: dark)").matches ? (
              <MoonStarIcon className="h-5 w-5" />
            ) : (
              <SunMoonIcon className="h-5 w-5" />
            )}
            <Trans>System</Trans>
            {themeMode === ThemeMode.System && <CheckIcon className="ml-auto h-5 w-5" />}
          </div>
        </MenuItem>
        <MenuItem id={ThemeMode.Light} textValue="Light">
          <div className="flex items-center gap-2">
            <SunIcon className="h-5 w-5" />
            <Trans>Light</Trans>
            {themeMode === ThemeMode.Light && <CheckIcon className="ml-auto h-5 w-5" />}
          </div>
        </MenuItem>
        <MenuItem id={ThemeMode.Dark} textValue="Dark">
          <div className="flex items-center gap-2">
            <MoonIcon className="h-5 w-5" />
            <Trans>Dark</Trans>
            {themeMode === ThemeMode.Dark && <CheckIcon className="ml-auto h-5 w-5" />}
          </div>
        </MenuItem>
      </Menu>
    </MenuTrigger>
  );

  return (
    <TooltipTrigger>
      {menuContent}
      <Tooltip>{t`Change theme`}</Tooltip>
    </TooltipTrigger>
  );
}