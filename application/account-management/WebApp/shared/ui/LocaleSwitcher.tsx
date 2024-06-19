import { ChevronDownIcon, LanguagesIcon } from "lucide-react";
import { Button, type Key, Label, ListBox, ListBoxItem, Popover, Select } from "react-aria-components";
import { translationContext, type Locale } from "@repo/infrastructure/translations/TranslationContext";
import { use, useCallback } from "react";
import { useLingui } from "@lingui/react";

export function LocaleSwitcher() {
  const { setLocale, getLocaleInfo, locales } = use(translationContext);
  const { i18n } = useLingui();

  const handleLocaleChange = useCallback(
    (newLocale: Key): void => {
      setLocale(newLocale as Locale);
    },
    [setLocale]
  );

  const currentLocale = i18n.locale as Locale;

  return (
    <Select onSelectionChange={handleLocaleChange} selectedKey={currentLocale} className="flex flex-col">
      <Label>Language</Label>
      <Button className="flex flex-row border border-border rounded p-2 justify-between">
        <LanguagesIcon />
        {getLocaleInfo(currentLocale).label}
        <ChevronDownIcon />
      </Button>
      <Popover className="border border-border rounded p-2 w-52 backdrop-blur-sm">
        <ListBox>
          {locales.map((locale) => (
            <ListBoxItem key={locale} id={locale} className="cursor-pointer p-2">
              {getLocaleInfo(locale).label}
            </ListBoxItem>
          ))}
        </ListBox>
      </Popover>
    </Select>
  );
}
