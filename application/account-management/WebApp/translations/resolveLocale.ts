import { locales, defaultLocale, LANGUAGE_COOKIE, parseLocale, pseudoLocale } from "./i18n";
import type { Locale } from "./i18n";
import { ReadonlyHeaders } from "next/dist/server/web/spec-extension/adapters/headers";
import Negotiator from "negotiator";
import { match as matchLocale } from "@formatjs/intl-localematcher";
import { RequestCookies } from "next/dist/server/web/spec-extension/cookies";

/**
 * Resolve valid locale from cookies and headers or use default locale
 */
export function resolveLocale(
  cookies: Pick<RequestCookies, "get">,
  headers: ReadonlyHeaders
): Locale {
  return (
    getUserLocale(cookies) ?? getLocaleOrDefault(getNegotiatedLocale(headers))
  );
}

/**
 * Get valid locale or default
 */
export function getLocaleOrDefault(locale: Locale | null): Locale {
  return locale != null && locales.includes(locale) ? locale : defaultLocale;
}

/**
 * Get valid negotiated locale from headers
 */
export function getNegotiatedLocale(headers: ReadonlyHeaders): Locale | null {
  const negotiatorHeaders: Record<string, string> = {};
  headers.forEach((value, key) => (negotiatorHeaders[key] = value));
  const availableLocales = locales.filter((locale) => locale !== pseudoLocale);

  let requestedLocales = new Negotiator({ headers: negotiatorHeaders }).languages(availableLocales);

  const negotiatedLocale = parseLocale(
    matchLocale(requestedLocales, availableLocales, defaultLocale)
  );
  return negotiatedLocale;
}

/**
 * Get valid user locale from cookie
 */
export function getUserLocale(
  cookies: Pick<RequestCookies, "get">
): Locale | null {
  const userLocale = parseLocale(cookies.get(LANGUAGE_COOKIE)?.value);
  if (userLocale != null && locales.includes(userLocale)) {
    return userLocale;
  }
  return null;
}
