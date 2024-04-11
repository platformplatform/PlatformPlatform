"use server";

import {
  LANGUAGE_COOKIE,
  defaultLocale,
  parseLocale,
} from "@/translations/i18n";
import { cookies } from "next/headers";

export type UserLocaleState = {
  message?: string;
  locale: string;
};

export async function setUserLocale(
  _: UserLocaleState,
  formData: FormData
): Promise<UserLocaleState> {
  const locale = parseLocale(formData.get("locale")?.toString());

  if (locale == null)
    return { message: "Invalid locale", locale: defaultLocale };

  cookies().set(LANGUAGE_COOKIE, locale);
  return { locale };
}
