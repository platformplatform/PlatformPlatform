import { cookies } from "next/headers";
import { z } from "zod";

export const THEME_MODE_COOKIE = "THEME_MODE";

export const ThemeModeScheme = z.enum(["light", "dark", "system"]);
export type ThemeMode = z.infer<typeof ThemeModeScheme>;

export type ThemeModeState = {
  message?: string;
  themeMode: "light" | "dark" | "system";
};

export function setThemeMode(themeModeInput?: string | null): ThemeMode {
  const result = ThemeModeScheme.safeParse(themeModeInput);

  if (!result.success) {
    return "system";
  }

  const themeMode = result.data;

  cookies().set(THEME_MODE_COOKIE, themeMode);
  return themeMode;
}

export function getThemeMode(): ThemeMode {
  const result = ThemeModeScheme.safeParse(
    cookies().get(THEME_MODE_COOKIE)?.value
  );

  if (!result.success) {
    return "system";
  }

  return result.data;
}
