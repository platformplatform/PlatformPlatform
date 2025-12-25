import { ThemeProvider as NextThemesProvider, useTheme } from "next-themes";
import type React from "react";
import { useEffect } from "react";

export { useTheme } from "next-themes";

type ThemeModeProviderProps = {
  children: React.ReactNode;
};

export function ThemeModeProvider({ children }: Readonly<ThemeModeProviderProps>) {
  return (
    <NextThemesProvider attribute="class" defaultTheme="system" enableSystem={true} disableTransitionOnChange={true}>
      <ThemeColorUpdater />
      {children}
    </NextThemesProvider>
  );
}

// Component to update theme-color meta tag for Dynamic Island
function ThemeColorUpdater() {
  const { resolvedTheme } = useTheme();

  useEffect(() => {
    const themeColorMeta = document.querySelector('meta[name="theme-color"]:not([media])') as HTMLMetaElement;
    const color = resolvedTheme === "dark" ? "#151b23" : "#eef0f2";

    if (themeColorMeta) {
      themeColorMeta.content = color;
    } else {
      const meta = document.createElement("meta");
      meta.name = "theme-color";
      meta.content = color;
      document.head.appendChild(meta);
    }
  }, [resolvedTheme]);

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
