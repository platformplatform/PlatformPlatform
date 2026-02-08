import { useLingui } from "@lingui/react";
import { Button } from "@repo/ui/components/Button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger
} from "@repo/ui/components/DropdownMenu";
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

  const handleLocaleChange = (locale: Locale) => {
    if (locale !== currentLocale) {
      setLocale(locale).then(() => {
        localStorage.setItem(preferredLocaleKey, locale);
      });
    }
  };

  const currentLocale = i18n.locale as Locale;

  return (
    <DropdownMenu>
      <DropdownMenuTrigger
        render={
          <Button variant="ghost" size="icon-lg" aria-label={ariaLabel}>
            <GlobeIcon className="size-5" />
          </Button>
        }
      />
      <DropdownMenuContent>
        {items.map((item) => (
          <DropdownMenuItem key={item.id} onClick={() => handleLocaleChange(item.id)}>
            <div className="flex items-center gap-2">
              <span>{item.label}</span>
              {item.id === currentLocale && <CheckIcon className="ml-auto size-4" />}
            </div>
          </DropdownMenuItem>
        ))}
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
