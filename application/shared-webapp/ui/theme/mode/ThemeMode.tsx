import type React from "react";

import { ThemeProvider as NextThemesProvider, useTheme } from "next-themes";
import { useEffect } from "react";

export { useTheme } from "next-themes";

// Brand-coded PWA toolbar tints, one per resolved theme mode. Passed in from the app shell so
// `@repo/ui` stays free of `@repo/infrastructure` imports (TS rootDir boundary -- see
// translationContext.ts). The app reads the values from `branding.themeColor` and threads them in.
type ThemeColor = {
  light: string;
  dark: string;
};

type ThemeModeProviderProps = {
  themeColor: ThemeColor;
  children: React.ReactNode;
};

export function ThemeModeProvider({ themeColor, children }: Readonly<ThemeModeProviderProps>) {
  return (
    <NextThemesProvider attribute="class" defaultTheme="system" enableSystem={true} disableTransitionOnChange={true}>
      <ThemeColorUpdater themeColor={themeColor} />
      {children}
    </NextThemesProvider>
  );
}

// Updates the PWA / iOS Dynamic Island toolbar tint (`<meta name="theme-color">`) to match the
// resolved theme mode.
function ThemeColorUpdater({ themeColor }: Readonly<{ themeColor: ThemeColor }>) {
  const { resolvedTheme } = useTheme();

  useEffect(() => {
    const themeColorMeta = document.querySelector('meta[name="theme-color"]:not([media])') as HTMLMetaElement;
    const color = resolvedTheme === "dark" ? themeColor.dark : themeColor.light;

    if (themeColorMeta) {
      themeColorMeta.content = color;
    } else {
      const meta = document.createElement("meta");
      meta.name = "theme-color";
      meta.content = color;
      document.head.appendChild(meta);
    }
  }, [resolvedTheme, themeColor]);

  return null;
}

function getSystemThemeMode(): "light" | "dark" {
  return window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light";
}

export function toggleThemeMode(theme: string | undefined): string {
  const currentTheme = theme ?? "system";
  const systemMode = getSystemThemeMode();

  // First click should go to opposite of system
  if (currentTheme === "system") {
    return systemMode === "dark" ? "light" : "dark";
  }

  // Second click should force the same mode as system
  if ((currentTheme === "light" && systemMode === "dark") || (currentTheme === "dark" && systemMode === "light")) {
    return currentTheme === "dark" ? "light" : "dark";
  }

  // Third click should go back to system
  return "system";
}
