import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import type { Key } from "@react-types/shared";
import { Button } from "@repo/ui/components/Button";
import { Menu, MenuItem, MenuTrigger } from "@repo/ui/components/Menu";
import { Tooltip, TooltipTrigger } from "@repo/ui/components/Tooltip";
import { CheckIcon, MoonIcon, MoonStarIcon, SunIcon, SunMoonIcon } from "lucide-react";
import { useEffect, useState } from "react";

const THEME_MODE_KEY = "preferred-theme";

enum ThemeMode {
  System = "system",
  Light = "light",
  Dark = "dark"
}

function updateThemeColorMeta() {
  requestAnimationFrame(() => {
    const root = document.documentElement;
    const computedStyle = window.getComputedStyle(root);
    const backgroundHsl = computedStyle.getPropertyValue("--background").trim();
    const backgroundColor = backgroundHsl ? `hsl(${backgroundHsl.replace(/\s+/g, ", ")})` : "#000000";

    const themeColorMetas = document.querySelectorAll('meta[name="theme-color"]');
    themeColorMetas.forEach((meta) => {
      meta.setAttribute("content", backgroundColor);
    });
  });
}

export default function ThemeModeSelector({
  variant = "icon",
  onAction
}: {
  variant?: "icon" | "mobile-menu";
  onAction?: () => void;
} = {}) {
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

    updateThemeColorMeta();

    // Listen for storage changes from other tabs/components
    const handleStorageChange = (e: StorageEvent) => {
      if (e.key === THEME_MODE_KEY && e.newValue) {
        const newMode = e.newValue as ThemeMode;
        if (Object.values(ThemeMode).includes(newMode)) {
          setThemeModeState(newMode);
        }
      }
    };

    // Listen for theme changes from the same tab (e.g., mobile menu)
    const handleThemeChange = (e: Event) => {
      const customEvent = e as CustomEvent;
      const newMode = customEvent.detail as ThemeMode;
      if (Object.values(ThemeMode).includes(newMode)) {
        setThemeModeState(newMode);
      }
    };

    window.addEventListener("storage", handleStorageChange);
    window.addEventListener("theme-mode-changed", handleThemeChange);

    return () => {
      window.removeEventListener("storage", handleStorageChange);
      window.removeEventListener("theme-mode-changed", handleThemeChange);
    };
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

    updateThemeColorMeta();

    // Dispatch event to notify other components
    window.dispatchEvent(new CustomEvent("theme-mode-changed", { detail: newMode }));

    // Call onAction callback if provided (for mobile menu)
    onAction?.();
  };

  const getThemeIcon = () => {
    switch (themeMode) {
      case ThemeMode.Dark:
        return <MoonIcon className="h-5 w-5" />;
      case ThemeMode.Light:
        return <SunIcon className="h-5 w-5" />;
      default: {
        // For system mode, show icon based on actual system preference
        const prefersDark = window.matchMedia("(prefers-color-scheme: dark)").matches;
        return prefersDark ? <MoonStarIcon className="h-5 w-5" /> : <SunMoonIcon className="h-5 w-5" />;
      }
    }
  };

  const menuContent = (
    <MenuTrigger>
      {variant === "icon" ? (
        <Button variant="icon" aria-label={t`Change theme`}>
          {getThemeIcon()}
        </Button>
      ) : (
        <Button
          variant="ghost"
          className="flex h-11 w-full items-center justify-start gap-4 px-3 py-2 font-normal text-base text-muted-foreground hover:bg-hover-background hover:text-foreground"
          style={{ pointerEvents: "auto" }}
        >
          <div className="flex h-6 w-6 shrink-0 items-center justify-center">{getThemeIcon()}</div>
          <div className="min-w-0 flex-1 overflow-hidden whitespace-nowrap text-start">
            <Trans>Theme</Trans>
          </div>
          <div className="shrink-0 text-base text-muted-foreground">
            {themeMode === ThemeMode.System ? (
              <Trans>System</Trans>
            ) : themeMode === ThemeMode.Light ? (
              <Trans>Light</Trans>
            ) : (
              <Trans>Dark</Trans>
            )}
          </div>
        </Button>
      )}
      <Menu
        onAction={handleThemeChange}
        aria-label={t`Change theme`}
        placement={variant === "mobile-menu" ? "bottom end" : "bottom"}
      >
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

  if (variant === "icon") {
    return (
      <TooltipTrigger>
        {menuContent}
        <Tooltip>{t`Change theme`}</Tooltip>
      </TooltipTrigger>
    );
  }

  return menuContent;
}
