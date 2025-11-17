import { useLingui } from "@lingui/react";
import type { Key } from "@react-types/shared";
import { Button } from "@repo/ui/components/Button";
import { Menu, MenuItem, MenuTrigger } from "@repo/ui/components/Menu";
import { CheckIcon, GlobeIcon } from "lucide-react";
import { use, useMemo } from "react";
import { preferredLocaleKey } from "./constants";
import { type Locale, translationContext } from "./TranslationContext";

export function LocaleSwitcher({ "aria-label": ariaLabel }: Readonly<{ "aria-label": string }>) {
  const { setLocale, getLocaleInfo, locales } = use(translationContext);
  const { i18n } = useLingui();

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
      setLocale(locale).then(() => {
        localStorage.setItem(preferredLocaleKey, locale);
      });
    }
  };

  const currentLocale = i18n.locale as Locale;

  return (
    <MenuTrigger>
      <Button variant="icon" aria-label={ariaLabel}>
        <GlobeIcon className="h-5 w-5" />
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
