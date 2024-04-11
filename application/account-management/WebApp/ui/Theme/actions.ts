"use server";

import { setThemeMode } from "./themeModeCookie";

export type ThemeModeState = {
  message?: string;
  themeMode: "light" | "dark" | "system";
};

export async function setThemeModeAction(
  _: ThemeModeState,
  formData: FormData
): Promise<ThemeModeState> {
  const themeMode = setThemeMode(formData.get("themeMode")?.toString());
  return { themeMode };
}
