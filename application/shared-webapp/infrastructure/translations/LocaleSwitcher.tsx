import { LanguagesIcon } from "lucide-react";
import { type Locale, translationContext } from "./TranslationContext";
import { use, useCallback, useMemo, useState } from "react";
import { useLingui } from "@lingui/react";
import { Button } from "@repo/ui/components/Button";
import { ListBox, ListBoxItem } from "@repo/ui/components/ListBox";
import { Popover } from "@repo/ui/components/Popover";
import { DialogTrigger } from "@repo/ui/components/Dialog";
import type { Selection } from "react-aria-components";

export function LocaleSwitcher() {
  const [isOpen, setIsOpen] = useState(false);
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

  const handleLocaleChange = useCallback(
    async (selection: Selection) => {
      const newLocale = [...selection][0] as Locale | undefined;
      if (newLocale != null) {
        await setLocale(newLocale);
      }
      setIsOpen(false);
    },
    [setLocale]
  );

  const currentLocale = i18n.locale as Locale;

  return (
    <DialogTrigger onOpenChange={setIsOpen} isOpen={isOpen}>
      <Button variant="icon">
        <LanguagesIcon />
      </Button>
      <Popover>
        <ListBox
          selectionMode="single"
          selectionBehavior="replace"
          onSelectionChange={handleLocaleChange}
          selectedKeys={[currentLocale]}
          className="border-none px-4 py-2"
          aria-label="Select a language"
        >
          {items.map((item) => (
            <ListBoxItem key={item.id} id={item.id}>
              {item.label}
            </ListBoxItem>
          ))}
        </ListBox>
      </Popover>
    </DialogTrigger>
  );
}

// onSelectionChange={handleLocaleChange} selectedKey={currentLocale} className="flex flex-col"
