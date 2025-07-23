import { t } from "@lingui/core/macro";
import type { Key } from "@react-types/shared";
import { enhancedFetch } from "@repo/infrastructure/http/httpClient";
import type { Locale } from "@repo/infrastructure/translations/TranslationContext";
import localeMap from "@repo/infrastructure/translations/i18n.config.json";
import { Button } from "@repo/ui/components/Button";
import { Menu, MenuItem, MenuTrigger } from "@repo/ui/components/Menu";
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

export default function LocaleSwitcher() {
  const [currentLocale, setCurrentLocale] = useState<Locale>("en-US");

  useEffect(() => {
    // Get current locale from document or localStorage
    const htmlLang = document.documentElement.lang as Locale;
    const savedLocale = localStorage.getItem(PREFERRED_LOCALE_KEY) as Locale;
    
    if (savedLocale && locales.some(l => l.id === savedLocale)) {
      setCurrentLocale(savedLocale);
    } else if (htmlLang && locales.some(l => l.id === htmlLang)) {
      setCurrentLocale(htmlLang);
    }
  }, []);

  const handleLocaleChange = async (key: Key) => {
    const locale = key.toString() as Locale;
    if (locale !== currentLocale) {
      // Save to localStorage
      localStorage.setItem(PREFERRED_LOCALE_KEY, locale);
      
      // Try to update backend
      await updateLocaleOnBackend(locale);
      
      // Reload page to apply new locale
      window.location.reload();
    }
  };

  const menuContent = (
    <MenuTrigger>
      <Button variant="icon" aria-label={t`Change language`}>
        <GlobeIcon className="h-5 w-5" />
      </Button>
      <Menu onAction={handleLocaleChange} aria-label={t`Change language`}>
        {locales.map((locale) => (
          <MenuItem key={locale.id} id={locale.id} textValue={locale.label}>
            <div className="flex items-center gap-2">
              <span>{locale.label}</span>
              {locale.id === currentLocale && <CheckIcon className="ml-auto h-4 w-4" />}
            </div>
          </MenuItem>
        ))}
      </Menu>
    </MenuTrigger>
  );

  return (
    <TooltipTrigger>
      {menuContent}
      <Tooltip>{t`Change language`}</Tooltip>
    </TooltipTrigger>
  );
}