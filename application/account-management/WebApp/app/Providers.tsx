"use client";

import { RouterProvider } from "react-aria-components";
import { AuthenticationProvider } from "@/lib/auth/AuthenticationProvider";
import { useRouter } from "next/navigation";
import { useLinguiInit } from "@/translations/useLinguiInit";
import { type Messages } from "@lingui/core";
import { I18nProvider } from "@lingui/react";
import { Locale } from "@/translations/i18n";
import { ThemeProvider } from "@/ui/Theme/ThemeContext";
import { ThemeMode } from "@/ui/Theme/themeModeCookie";

type ProvidersProps = {
  children: React.ReactNode;
  messages: Messages;
  locale: Locale;
  themeMode: ThemeMode;
};

export function Providers({
  children,
  messages,
  locale,
  themeMode,
}: Readonly<ProvidersProps>) {
  const initializedI18n = useLinguiInit(messages, locale);
  const { push } = useRouter();

  return (
    <ThemeProvider initialThemeMode={themeMode}>
      <I18nProvider i18n={initializedI18n}>
        <RouterProvider navigate={push}>
          <AuthenticationProvider navigate={push} afterSignIn="/dashboard" afterSignOut="/login">
            {children}
          </AuthenticationProvider>
        </RouterProvider>
      </I18nProvider>
    </ThemeProvider>
  );
}
