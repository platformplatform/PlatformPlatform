import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { useIsAuthenticated } from "@repo/infrastructure/auth/hooks";
import { enhancedFetch } from "@repo/infrastructure/http/httpClient";
import localeMap from "@repo/infrastructure/translations/i18n.config.json";
import type { Locale } from "@repo/infrastructure/translations/TranslationContext";
import { Button } from "@repo/ui/components/Button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger
} from "@repo/ui/components/DropdownMenu";
import { Tooltip, TooltipTrigger } from "@repo/ui/components/Tooltip";
import { CheckIcon, GlobeIcon } from "lucide-react";
import { useEffect, useState } from "react";

const PREFERRED_LOCALE_KEY = "preferred-locale";

const locales = Object.entries(localeMap).map(([id, info]) => ({
  id: id as Locale,
  label: info.label
}));

async function updateLocaleOnBackend(locale: Locale) {
  try {
    const response = await enhancedFetch("/api/account-management/users/me/change-locale", {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ Locale: locale })
    });

    return response.ok || response.status === 401;
  } catch {
    return true; // Continue even if API call fails
  }
}

export default function LocaleSwitcher({
  variant = "icon",
  onAction
}: {
  variant?: "icon" | "mobile-menu";
  onAction?: () => void;
} = {}) {
  const [currentLocale, setCurrentLocale] = useState<Locale>("en-US");
  const isAuthenticated = useIsAuthenticated();

  useEffect(() => {
    // Get current locale from document or localStorage
    const htmlLang = document.documentElement.lang as Locale;
    const savedLocale = localStorage.getItem(PREFERRED_LOCALE_KEY) as Locale;

    if (savedLocale && locales.some((l) => l.id === savedLocale)) {
      setCurrentLocale(savedLocale);
    } else if (htmlLang && locales.some((l) => l.id === htmlLang)) {
      setCurrentLocale(htmlLang);
    }
  }, []);

  const handleLocaleChange = async (locale: Locale) => {
    if (locale !== currentLocale) {
      // Call onAction if provided (for closing mobile menu)
      onAction?.();

      // Save to localStorage
      localStorage.setItem(PREFERRED_LOCALE_KEY, locale);

      // Only update backend if user is authenticated
      if (isAuthenticated) {
        await updateLocaleOnBackend(locale);
      }

      // Reload page to apply new locale
      window.location.reload();
    }
  };

  const currentLocaleLabel = locales.find((l) => l.id === currentLocale)?.label || currentLocale;

  if (variant === "mobile-menu") {
    return (
      <DropdownMenu>
        <DropdownMenuTrigger
          render={
            <Button
              variant="ghost"
              className="flex h-11 w-full items-center justify-start gap-4 px-3 py-2 font-normal text-base text-muted-foreground hover:bg-hover-background hover:text-foreground"
              style={{ pointerEvents: "auto" }}
            >
              <div className="flex h-6 w-6 shrink-0 items-center justify-center">
                <GlobeIcon className="h-5 w-5 stroke-current" />
              </div>
              <div className="min-w-0 flex-1 overflow-hidden whitespace-nowrap text-start">
                <Trans>Language</Trans>
              </div>
              <div className="shrink-0 text-base text-muted-foreground">{currentLocaleLabel}</div>
            </Button>
          }
        />
        <DropdownMenuContent align="end">
          {locales.map((locale) => (
            <DropdownMenuItem key={locale.id} onClick={() => handleLocaleChange(locale.id)}>
              <div className="flex items-center gap-2">
                <span>{locale.label}</span>
                {locale.id === currentLocale && <CheckIcon className="ml-auto h-4 w-4" />}
              </div>
            </DropdownMenuItem>
          ))}
        </DropdownMenuContent>
      </DropdownMenu>
    );
  }

  // Icon variant
  const menuContent = (
    <DropdownMenu>
      <DropdownMenuTrigger
        render={
          <Button variant="ghost" size="icon-lg" aria-label={t`Change language`}>
            <GlobeIcon className="size-5" />
          </Button>
        }
      />
      <DropdownMenuContent>
        {locales.map((locale) => (
          <DropdownMenuItem key={locale.id} onClick={() => handleLocaleChange(locale.id)}>
            <div className="flex items-center gap-2">
              <span>{locale.label}</span>
              {locale.id === currentLocale && <CheckIcon className="ml-auto h-4 w-4" />}
            </div>
          </DropdownMenuItem>
        ))}
      </DropdownMenuContent>
    </DropdownMenu>
  );

  return (
    <TooltipTrigger>
      {menuContent}
      <Tooltip>{t`Change language`}</Tooltip>
    </TooltipTrigger>
  );
}
