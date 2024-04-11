"use client";
import { i18n, Messages } from "@lingui/core";
import { useEffect } from "react";
import { Locale } from "./i18n";

export function useLinguiInit(messages: Messages, locale: Locale) {
  if (i18n.locale !== locale) {
    i18n.loadAndActivate({ locale, messages });
  }

  useEffect(() => {
    const localeDidChange = locale !== i18n.locale;
    if (localeDidChange) {
      i18n.loadAndActivate({ locale, messages });
    }
  }, [locale, messages]);

  return i18n;
}
