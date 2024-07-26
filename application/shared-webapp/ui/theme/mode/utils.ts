export type SystemThemeMode = "light" | "dark";
export type ThemeMode = SystemThemeMode | "system";

export function sanitizeThemeMode(themeMode: string | null): ThemeMode {
  return themeMode === "light" || themeMode === "dark" ? themeMode : "system";
}

export function resolveThemeMode(...modes: ThemeMode[]): SystemThemeMode {
  for (const mode of modes) {
    if (mode === "light" || mode === "dark") {
      return mode;
    }
  }

  return "light";
}

export const setClassNameThemeMode = (mode: ThemeMode) => {
  document.documentElement.classList.remove("light", "dark");
  if (mode === "dark" || mode === "light") {
    document.documentElement.classList.add(mode);
    document.documentElement.style.colorScheme = mode;
  } else {
    document.documentElement.style.removeProperty("color-scheme");
  }
};

export function getClassNameThemeMode() {
  if (document.documentElement.classList.contains("light")) {
    return "light";
  }
  if (document.documentElement.classList.contains("dark")) {
    return "dark";
  }
  return "system";
}
