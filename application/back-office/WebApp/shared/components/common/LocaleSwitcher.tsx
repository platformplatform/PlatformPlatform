import { t } from "@lingui/core/macro";
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
import { Tooltip, TooltipContent, TooltipTrigger } from "@repo/ui/components/Tooltip";
import { CheckIcon, GlobeIcon } from "lucide-react";
import { useEffect, useState } from "react";

const PREFERRED_LOCALE_KEY = "preferred-locale";

const locales = Object.entries(localeMap).map(([id, info]) => ({
  id: id as Locale,
  label: info.label
}));

async function updateLocaleOnBackend(locale: Locale) {
  try {
    const response = await enhancedFetch("/api/account/users/me/change-locale", {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ Locale: locale })
    });

    return response.ok || response.status === 401;
  } catch {
    return true;
  }
}

export function LocaleSwitcher() {
  const [currentLocale, setCurrentLocale] = useState<Locale>("en-US");
  const isAuthenticated = useIsAuthenticated();

  useEffect(() => {
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
      localStorage.setItem(PREFERRED_LOCALE_KEY, locale);

      if (isAuthenticated) {
        await updateLocaleOnBackend(locale);
      }

      window.location.reload();
    }
  };

  return (
    <DropdownMenu>
      <Tooltip>
        <TooltipTrigger
          render={
            <DropdownMenuTrigger
              render={
                <Button variant="ghost" size="icon" aria-label={t`Change language`}>
                  <GlobeIcon className="size-5" />
                </Button>
              }
            />
          }
        />
        <TooltipContent>{t`Change language`}</TooltipContent>
      </Tooltip>
      <DropdownMenuContent>
        {locales.map((locale) => (
          <DropdownMenuItem key={locale.id} onClick={() => handleLocaleChange(locale.id)}>
            <div className="flex items-center gap-2">
              <span>{locale.label}</span>
              {locale.id === currentLocale && <CheckIcon className="ml-auto size-4" />}
            </div>
          </DropdownMenuItem>
        ))}
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
