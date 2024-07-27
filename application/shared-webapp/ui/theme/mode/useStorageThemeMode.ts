import { useCallback, useEffect, useState } from "react";
import { type ThemeMode, sanitizeThemeMode } from "./utils";

const STORAGE_KEY = "themeMode";

export function getStorageThemeMode(): ThemeMode {
  return sanitizeThemeMode(localStorage.getItem(STORAGE_KEY));
}

export function useStorageThemeMode(): [ThemeMode, (mode: ThemeMode) => void] {
  const [themeMode, setThemeMode] = useState<ThemeMode>(getStorageThemeMode());

  const setStorageThemeMode = useCallback((mode: ThemeMode) => {
    if (mode === "system") {
      localStorage.removeItem(STORAGE_KEY);
    } else {
      localStorage.setItem(STORAGE_KEY, mode);
    }
    setThemeMode(mode);
  }, []);

  useEffect(() => {
    // Listen for changes in other tabs
    const storageListener = (e: StorageEvent) => {
      if (e.key === STORAGE_KEY) {
        setThemeMode(sanitizeThemeMode(e.newValue));
      }
    };
    window.addEventListener("storage", storageListener);
    return () => window.removeEventListener("storage", storageListener);
  }, []);

  return [themeMode, setStorageThemeMode];
}
