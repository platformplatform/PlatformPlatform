"use client";

import { createContext, useCallback, useContext, useMemo } from "react";
import { setThemeModeAction } from "./actions";
import { useFormState } from "react-dom";
import { ThemeMode } from "./themeModeCookie";

export type ThemeModeSetter = (mode: ThemeMode) => void;
export type ThemeModeContext = {
  themeMode: ThemeMode;
  setThemeMode: ThemeModeSetter;
};

export const ThemeContext = createContext<ThemeModeContext>({
  themeMode: "system",
  setThemeMode: () => {},
});

type ThemeProviderProps = {
  initialThemeMode?: ThemeMode;
  children: React.ReactNode;
};

export function ThemeProvider({
  initialThemeMode = "system",
  children,
}: Readonly<ThemeProviderProps>) {
  const [state, formAction] = useFormState(setThemeModeAction, {
    themeMode: initialThemeMode,
  });

  const handleSetThemeMode: ThemeModeSetter = useCallback(
    (mode) => {
      const data = new FormData();
      data.append("themeMode", mode);
      formAction(data);
      updateThemeMode(mode);
    },
    [formAction]
  );

  const themeContextValue = useMemo(
    () => ({
      themeMode: state.themeMode,
      setThemeMode: handleSetThemeMode,
    }),
    [state.themeMode, handleSetThemeMode]
  );

  return (
    <ThemeContext.Provider value={themeContextValue}>
      {children}
    </ThemeContext.Provider>
  );
}

export function useThemeMode() {
  return useContext(ThemeContext);
}

function updateThemeMode(themeMode: ThemeMode) {
  if (themeMode === "dark") {
    document.documentElement.classList.add("dark");
    document.documentElement.classList.remove("light");
  } else if (themeMode === "light") {
    document.documentElement.classList.remove("dark");
    document.documentElement.classList.add("light");
  }
}
