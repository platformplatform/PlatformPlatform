import { useEffect, useState } from "react";
import { SystemThemeMode } from "./utils";

const mq = window.matchMedia("(prefers-color-scheme: dark)");

export function getSystemThemeMode(): SystemThemeMode {
  return mq.matches ? SystemThemeMode.Dark : SystemThemeMode.Light;
}

/**
 * React Hook to get the current system theme mode.
 */
export function useSystemThemeMode() {
  const [themeMode, setThemeMode] = useState<SystemThemeMode>(getSystemThemeMode());

  useEffect(() => {
    const listener = () => setThemeMode(getSystemThemeMode());
    mq.addEventListener("change", listener);
    return () => mq.removeEventListener("change", listener);
  }, []);

  return themeMode;
}
