import { useLingui } from "@lingui/react";
import { ChevronDownIcon, LanguagesIcon } from "lucide-react";
import { Button, type Key, Label, ListBox, ListBoxItem, Popover, Select } from "react-aria-components";
import { type Locale, dynamicActivate, getLanguage, locales } from "@/translations/i18n";

export function LocaleSwitcher(): JSX.Element {
  const { i18n } = useLingui();

  const handleLocaleChange = (newLocale: Key): void => {
    void dynamicActivate(i18n, newLocale as Locale);
  };

  const currentLocale = i18n.locale as Locale;

  return (
    <Select onSelectionChange={handleLocaleChange} selectedKey={currentLocale} className="flex flex-col">
      <Label>Language</Label>
      <Button className="flex flex-row border border-border rounded p-2 justify-between">
        <LanguagesIcon />
        {getLanguage(currentLocale).label}
        <ChevronDownIcon />
      </Button>
      <Popover className="border border-border rounded p-2 w-52 backdrop-blur-sm">
        <ListBox>
          {locales.map(locale => (
            <ListBoxItem key={locale} id={locale} className="cursor-pointer p-2">
              {getLanguage(locale).label}
            </ListBoxItem>
          ))}
        </ListBox>
      </Popover>
    </Select>
  );
}
