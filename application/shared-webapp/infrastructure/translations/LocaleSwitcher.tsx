import { useLingui } from "@lingui/react";
import type { Key } from "@react-types/shared";
import { AuthenticationContext } from "@repo/infrastructure/auth/AuthenticationProvider";
import { fetchWithAntiforgeryToken } from "@repo/infrastructure/http/antiforgeryTokenHandler";
import { Button } from "@repo/ui/components/Button";
import { Menu, MenuItem, MenuTrigger } from "@repo/ui/components/Menu";
import { CheckIcon, LanguagesIcon } from "lucide-react";
import { use, useContext, useMemo } from "react";
import { type Locale, translationContext } from "./TranslationContext";
import { preferredLocaleKey } from "./constants";

export function LocaleSwitcher({ "aria-label": ariaLabel }: { "aria-label": string }) {
  const { setLocale, getLocaleInfo, locales } = use(translationContext);
  const { i18n } = useLingui();
  const { userInfo } = useContext(AuthenticationContext);

  const items = useMemo(
    () =>
      locales.map((locale) => ({
        id: locale,
        label: getLocaleInfo(locale).label
      })),
    [locales, getLocaleInfo]
  );

  const handleLocaleChange = (key: Key) => {
    const locale = key.toString() as Locale;
    if (locale !== currentLocale) {
      if (userInfo?.isAuthenticated) {
        fetchWithAntiforgeryToken("/api/account-management/users/me/change-locale", {
          method: "PUT",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ locale })
        })
          .then(async (_) => {
            await setLocale(locale);
            localStorage.setItem(preferredLocaleKey, locale);
          })
          .catch((error) => console.error("Failed to update locale:", error));
      } else {
        setLocale(locale).then(() => {
          localStorage.setItem(preferredLocaleKey, locale);
        });
      }
    }
  };

  const currentLocale = i18n.locale as Locale;

  return (
    <MenuTrigger>
      <Button variant="icon" aria-label={ariaLabel}>
        <LanguagesIcon className="h-5 w-5" />
      </Button>
      <Menu onAction={handleLocaleChange} aria-label={ariaLabel}>
        {items.map((item) => (
          <MenuItem key={item.id} id={item.id} textValue={item.label}>
            <div className="flex items-center gap-2">
              <span>{item.label}</span>
              {item.id === currentLocale && <CheckIcon className="ml-auto h-4 w-4" />}
            </div>
          </MenuItem>
        ))}
      </Menu>
    </MenuTrigger>
  );
}
