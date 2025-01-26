import { LanguagesIcon, CheckIcon } from "lucide-react";
import { type Locale, translationContext } from "./TranslationContext";
import { use, useContext, useMemo, useState } from "react";
import { useLingui } from "@lingui/react";
import { Button } from "@repo/ui/components/Button";
import { ListBox, ListBoxItem } from "@repo/ui/components/ListBox";
import { Popover } from "@repo/ui/components/Popover";
import { DialogTrigger } from "@repo/ui/components/Dialog";
import type { Selection } from "react-aria-components";
import { AuthenticationContext } from "@repo/infrastructure/auth/AuthenticationProvider";
import { preferredLocaleKey } from "./constants";

export function LocaleSwitcher() {
  const [isOpen, setIsOpen] = useState(false);
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

  const handleLocaleChange = async (selection: Selection) => {
    setIsOpen(false);

    const newLocale = [...selection][0] as Locale;
    if (newLocale != null && newLocale !== currentLocale) {
      if (userInfo?.isAuthenticated) {
        await fetch("/api/account-management/users/me/change-locale", {
          method: "PUT",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ locale: newLocale })
        })
          .then(async (_) => {
            await setLocale(newLocale);
            localStorage.setItem(preferredLocaleKey, newLocale);
          })
          .catch((error) => console.error("Failed to update locale:", error));
      } else {
        await setLocale(newLocale);
        localStorage.setItem(preferredLocaleKey, newLocale);
      }
    }
  };

  const currentLocale = i18n.locale as Locale;

  return (
    <DialogTrigger onOpenChange={setIsOpen} isOpen={isOpen}>
      <Button variant="icon">
        <LanguagesIcon />
      </Button>
      <Popover>
        <ListBox
          selectionMode="single"
          onSelectionChange={handleLocaleChange}
          selectedKeys={[currentLocale]}
          className="border-none px-4 py-2"
          aria-label="Select a language"
          autoFocus
        >
          {items.map((item) => (
            <ListBoxItem key={item.id} id={item.id}>
              <div className="flex items-center justify-between w-full">
                <span>{item.label}</span>
                {item.id === currentLocale && <CheckIcon className="h-4 w-4" />}
              </div>
            </ListBoxItem>
          ))}
        </ListBox>
      </Popover>
    </DialogTrigger>
  );
}
