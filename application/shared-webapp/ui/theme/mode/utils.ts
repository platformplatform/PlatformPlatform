export enum SystemThemeMode {
  Light = "light",
  Dark = "dark"
}

export enum ThemeMode {
  System = "system",
  Light = "light",
  Dark = "dark"
}

export function sanitizeThemeMode(themeMode: string | null): ThemeMode {
  return themeMode === ThemeMode.Light || themeMode === ThemeMode.Dark ? (themeMode as ThemeMode) : ThemeMode.System;
}

export function resolveThemeMode(...modes: (ThemeMode | SystemThemeMode)[]): SystemThemeMode {
  for (const mode of modes) {
    if (mode === ThemeMode.Light) {
      return SystemThemeMode.Light;
    }
    if (mode === ThemeMode.Dark) {
      return SystemThemeMode.Dark;
    }
  }

  return SystemThemeMode.Light;
}

export const setClassNameThemeMode = (mode: ThemeMode) => {
  document.documentElement.classList.remove(SystemThemeMode.Light, SystemThemeMode.Dark);
  if (mode === ThemeMode.Dark || mode === ThemeMode.Light) {
    document.documentElement.classList.add(mode);
    document.documentElement.style.colorScheme = mode;

    // Update theme-color meta tag for Dynamic Island
    const themeColorMeta = document.querySelector('meta[name="theme-color"]:not([media])') as HTMLMetaElement;
    if (themeColorMeta) {
      themeColorMeta.content = mode === ThemeMode.Dark ? "#151b23" : "#eef0f2";
    } else {
      const meta = document.createElement("meta");
      meta.name = "theme-color";
      meta.content = mode === ThemeMode.Dark ? "#151b23" : "#eef0f2";
      document.head.appendChild(meta);
    }
  } else {
    document.documentElement.style.removeProperty("color-scheme");
    // Remove non-media theme-color when in system mode
    const themeColorMeta = document.querySelector('meta[name="theme-color"]:not([media])') as HTMLMetaElement;
    if (themeColorMeta) {
      themeColorMeta.remove();
    }
  }
};

export function getClassNameThemeMode(): ThemeMode {
  if (document.documentElement.classList.contains(SystemThemeMode.Light)) {
    return ThemeMode.Light;
  }
  if (document.documentElement.classList.contains(SystemThemeMode.Dark)) {
    return ThemeMode.Dark;
  }
  return ThemeMode.System;
}
