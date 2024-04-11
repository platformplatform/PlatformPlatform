"use client";

import { useState } from "react";
import { useFormState } from "react-dom";
import { useLingui } from "@lingui/react";
import { Locale, locales, getLanguage, parseLocale } from "@/translations/i18n";
import { LanguagesIcon } from "lucide-react";
import { Button } from "../components/Button";
import { Popover } from "../components/Popover";
import { Dialog } from "../components/Dialog";
import { DialogTrigger, Selection } from "react-aria-components";
import { getLocaleOrDefault } from "@/translations/resolveLocale";
import { setUserLocale as setUserLocaleAction } from "./actions";
import { loadCatalog } from "@/translations/loadCatalog";
import { ListBox, ListBoxItem } from "../components/ListBox";

const languages = locales.reduce(
  (result, locale) => ({
    ...result,
    [locale]: getLanguage(locale).label,
  }),
  {} as Record<Locale, string>
);

export function LanguageSwitcher() {
  const { i18n } = useLingui();
  const [open, setOpen] = useState(false);
  const [state, formAction] = useFormState(setUserLocaleAction, {
    locale: i18n.locale,
  });

  async function handleChange(localeInput: string) {
    const locale = getLocaleOrDefault(parseLocale(localeInput));
    const messages = await loadCatalog(locale);
    i18n.loadAndActivate({ locale, messages });

    // Update cookie
    const data = new FormData();
    data.append("locale", locale);
    formAction(data);
  }

  function handleLanguageChange(keys: Selection) {
    if (keys instanceof Set) {
      const locale = keys.values().next().value;
      handleChange(locale);
      setOpen(false);
    }
  }

  return (
    <DialogTrigger isOpen={open} onOpenChange={setOpen}>
      <Button variant="icon">
        <LanguagesIcon />
      </Button>
      <Popover showArrow isOpen={open} onOpenChange={setOpen}>
        <Dialog className="w-44">
          <ListBox
            className="border-none"
            aria-label="Theme mode"
            selectionMode="single"
            selectedKeys={[state.locale]}
            onSelectionChange={handleLanguageChange}
          >
            {Object.entries(languages).map(([locale, label]) => (
              <ListBoxItem key={locale} id={locale}>
                {label}
              </ListBoxItem>
            ))}
          </ListBox>
        </Dialog>
      </Popover>
    </DialogTrigger>
  );
}
