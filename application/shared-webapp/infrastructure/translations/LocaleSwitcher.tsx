import { LanguagesIcon, CheckIcon } from "lucide-react";
import { type Locale, translationContext } from "./TranslationContext";
import { use, useContext, useMemo } from "react";
import { useLingui } from "@lingui/react";
import { Button } from "@repo/ui/components/Button";
import { Menu, MenuItem, MenuTrigger } from "@repo/ui/components/Menu";
import { AuthenticationContext } from "@repo/infrastructure/auth/AuthenticationProvider";
import { preferredLocaleKey } from "./constants";
import type { Key } from "@react-types/shared";

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
        void fetch("/api/account-management/users/me/change-locale", {
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
        void setLocale(locale).then(() => {
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
              {item.id === currentLocale && <CheckIcon className="h-4 w-4 ml-auto" />}
            </div>
          </MenuItem>
        ))}
      </Menu>
    </MenuTrigger>
  );
}
