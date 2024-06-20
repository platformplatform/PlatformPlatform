import { createContext, useCallback, useContext, useMemo, useState } from "react";

const storageKey = "themeMode";
type ThemeMode = "light" | "dark";

export type ThemeModeContextType = [ThemeMode, (mode: ThemeMode) => void];

export const ThemeModeContext = createContext<ThemeModeContextType>(["light", () => {}]);

const setClassName = (mode: ThemeMode) => {
  document.body.classList.remove("light", "dark");
  document.body.classList.add(mode);
};

const initialThemeMode = localStorage.getItem(storageKey) === "dark" ? "dark" : "light";
setClassName(initialThemeMode);

type ThemeModeProviderProps = {
  children: React.ReactNode;
};

export function ThemeModeProvider({ children }: Readonly<ThemeModeProviderProps>) {
  const [themeMode, setThemeMode] = useState<ThemeMode>(initialThemeMode);

  const setAndStoreThemeMode = useCallback((mode: ThemeMode) => {
    localStorage.setItem(storageKey, mode);
    setClassName(mode);
    setThemeMode(mode);
  }, []);

  const value = useMemo<ThemeModeContextType>(
    () => [themeMode, setAndStoreThemeMode],
    [themeMode, setAndStoreThemeMode]
  );

  return <ThemeModeContext.Provider value={value}>{children}</ThemeModeContext.Provider>;
}

/**
 * Hook to get the current theme mode and a function to set it.
 */
export const useThemeMode = () => useContext(ThemeModeContext);
