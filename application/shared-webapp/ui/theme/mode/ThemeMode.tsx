import type React from "react";
import { createContext, useContext, useEffect, useMemo } from "react";
import { getStorageThemeMode, useStorageThemeMode } from "./useStorageThemeMode";
import { getSystemThemeMode, useSystemThemeMode } from "./useSystemThemeMode";
import { type SystemThemeMode, type ThemeMode, resolveThemeMode, setClassNameThemeMode } from "./utils";

type ThemeModeSetter = (mode: ThemeMode) => ThemeMode;

export type ThemeModeContextType = {
  themeMode: ThemeMode;
  resolvedThemeMode: SystemThemeMode;
  setThemeMode: (mode: ThemeMode | ThemeModeSetter) => void;
};

export const ThemeModeContext = createContext<ThemeModeContextType>({
  themeMode: "system",
  resolvedThemeMode: "light",
  setThemeMode: () => {}
});

const getInitialThemeMode = () => resolveThemeMode(getStorageThemeMode(), getSystemThemeMode());

// Set the initial theme mode on the document element as soon as possible
setClassNameThemeMode(getInitialThemeMode());

type ThemeModeProviderProps = {
  children: React.ReactNode;
};

export function ThemeModeProvider({ children }: Readonly<ThemeModeProviderProps>) {
  const systemThemeMode = useSystemThemeMode();
  const [storageThemeMode, setStorageThemeMode] = useStorageThemeMode();

  const resolvedThemeMode = useMemo(
    () => resolveThemeMode(storageThemeMode, systemThemeMode),
    [storageThemeMode, systemThemeMode]
  );

  useEffect(() => setClassNameThemeMode(resolvedThemeMode), [resolvedThemeMode]);

  const value = useMemo<ThemeModeContextType>(
    () => ({
      themeMode: storageThemeMode,
      resolvedThemeMode,
      setThemeMode: (newMode) => {
        const nextMode = typeof newMode === "function" ? newMode(storageThemeMode) : newMode;
        setStorageThemeMode(nextMode);
      }
    }),
    [resolvedThemeMode, storageThemeMode, setStorageThemeMode]
  );

  return <ThemeModeContext.Provider value={value}>{children}</ThemeModeContext.Provider>;
}

/**
 * Hook to get the current theme mode and a function to set it.
 */
export const useThemeMode = () => useContext(ThemeModeContext);

export function toggleThemeMode(mode: ThemeMode): ThemeMode {
  const systemMode = getSystemThemeMode();
  if (mode === "light") {
    return systemMode === "dark" ? "system" : "dark";
  }
  if (mode === "dark") {
    return systemMode === "light" ? "system" : "light";
  }
  // mode was system so toggle between light and dark
  return systemMode === "dark" ? "light" : "dark";
}
