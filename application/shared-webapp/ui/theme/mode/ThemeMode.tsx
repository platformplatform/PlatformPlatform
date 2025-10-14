import type React from "react";
import { createContext, useContext, useEffect, useMemo } from "react";
import { getStorageThemeMode, useStorageThemeMode } from "./useStorageThemeMode";
import { getSystemThemeMode, useSystemThemeMode } from "./useSystemThemeMode";
import { resolveThemeMode, SystemThemeMode, setClassNameThemeMode, ThemeMode } from "./utils";

type ThemeModeSetter = (mode: ThemeMode) => ThemeMode;

export type ThemeModeContextType = {
  themeMode: ThemeMode;
  resolvedThemeMode: SystemThemeMode;
  setThemeMode: (mode: ThemeMode | ThemeModeSetter) => void;
};

export const ThemeModeContext = createContext<ThemeModeContextType>({
  themeMode: ThemeMode.System,
  resolvedThemeMode: SystemThemeMode.Light,
  setThemeMode: () => {}
});

// Helper to convert SystemThemeMode to ThemeMode
const systemToThemeMode = (mode: SystemThemeMode): ThemeMode => {
  return mode === SystemThemeMode.Dark ? ThemeMode.Dark : ThemeMode.Light;
};

// Get initial theme mode
const initialStorageMode = getStorageThemeMode();
const initialSystemMode = getSystemThemeMode();
const resolvedSystemMode = resolveThemeMode(initialStorageMode, systemToThemeMode(initialSystemMode));

// Convert SystemThemeMode to ThemeMode for setClassNameThemeMode
const resolvedInitialMode = resolvedSystemMode === SystemThemeMode.Dark ? ThemeMode.Dark : ThemeMode.Light;

// Set the initial theme mode on the document element as soon as possible
setClassNameThemeMode(resolvedInitialMode);

type ThemeModeProviderProps = {
  children: React.ReactNode;
};

export function ThemeModeProvider({ children }: Readonly<ThemeModeProviderProps>) {
  const systemThemeMode = useSystemThemeMode();
  const [storageThemeMode, setStorageThemeMode] = useStorageThemeMode();

  const resolvedThemeMode = useMemo(
    () => resolveThemeMode(storageThemeMode, systemToThemeMode(systemThemeMode)),
    [storageThemeMode, systemThemeMode]
  );

  // Convert SystemThemeMode to ThemeMode for setClassNameThemeMode
  useEffect(() => {
    const themeMode = resolvedThemeMode === SystemThemeMode.Dark ? ThemeMode.Dark : ThemeMode.Light;
    setClassNameThemeMode(themeMode);
  }, [resolvedThemeMode]);

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

  // First click should go to opposite of system
  if (mode === ThemeMode.System) {
    return systemMode === SystemThemeMode.Dark ? ThemeMode.Light : ThemeMode.Dark;
  }

  // Second click should force the same mode as system
  if (
    (mode === ThemeMode.Light && systemMode === SystemThemeMode.Dark) ||
    (mode === ThemeMode.Dark && systemMode === SystemThemeMode.Light)
  ) {
    return mode === ThemeMode.Dark ? ThemeMode.Light : ThemeMode.Dark;
  }

  // Third click should go back to system
  return ThemeMode.System;
}
