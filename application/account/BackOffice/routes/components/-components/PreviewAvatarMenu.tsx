import { t } from "@lingui/core/macro";
import { useLingui } from "@lingui/react";
import { Trans } from "@lingui/react/macro";
import { preferredLocaleKey } from "@repo/infrastructure/translations/constants";
import localeMap from "@repo/infrastructure/translations/i18n.config.json";
import { type Locale, translationContext } from "@repo/infrastructure/translations/TranslationContext";
import { Avatar, AvatarFallback } from "@repo/ui/components/Avatar";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuGroup,
  DropdownMenuItem,
  DropdownMenuSub,
  DropdownMenuSubContent,
  DropdownMenuSubTrigger,
  DropdownMenuTrigger
} from "@repo/ui/components/DropdownMenu";
import { collapsedContext } from "@repo/ui/components/Sidebar";
import { SIDE_MENU_DEFAULT_WIDTH_REM } from "@repo/ui/utils/responsive";
import {
  CheckIcon,
  ChevronsUpDownIcon,
  GlobeIcon,
  MoonIcon,
  MoonStarIcon,
  PencilIcon,
  SunIcon,
  SunMoonIcon,
  ZoomInIcon
} from "lucide-react";
import { useTheme } from "next-themes";
import { use, useContext, useEffect, useState } from "react";

const zoomLevelStorageKey = "zoom-level";

const zoomLevelOptions = [
  { value: "0.875", label: () => t`Small` },
  { value: "1", label: () => t`Default` },
  { value: "1.125", label: () => t`Large` },
  { value: "1.25", label: () => t`Larger` }
];

function FakeProfileCard() {
  const [isHighlighted, setIsHighlighted] = useState(false);

  return (
    <DropdownMenuGroup>
      <DropdownMenuItem
        className="flex flex-col items-center gap-1 px-4 py-3"
        onMouseEnter={() => setIsHighlighted(true)}
        onMouseLeave={() => setIsHighlighted(false)}
      >
        <div className="relative">
          <Avatar className="size-16">
            <AvatarFallback className="text-xl">PP</AvatarFallback>
          </Avatar>
          <div
            className="pointer-events-none absolute -right-0.5 -bottom-0.5 flex size-5 items-center justify-center rounded-full border border-border [&_*]:!text-inherit"
            style={{
              backgroundColor: isHighlighted ? "var(--color-primary)" : "var(--color-popover)",
              color: isHighlighted ? "var(--color-primary-foreground)" : "var(--color-muted-foreground)"
            }}
          >
            <PencilIcon className="size-2.5" strokeWidth={3} />
          </div>
        </div>
        <span className="font-medium">
          <Trans>PlatformPlatform</Trans>
        </span>
        {isHighlighted ? (
          <span className="text-sm">
            <Trans>Edit profile</Trans>
          </span>
        ) : (
          <span className="text-sm text-muted-foreground">preview@platformplatform.net</span>
        )}
      </DropdownMenuItem>
    </DropdownMenuGroup>
  );
}

export function PreviewAvatarMenu() {
  const isCollapsed = useContext(collapsedContext);
  const { theme, setTheme, resolvedTheme } = useTheme();
  const { setLocale } = use(translationContext);
  const locales = Object.keys(localeMap) as Locale[];
  const getLocaleInfo = (locale: Locale) => localeMap[locale];
  const { i18n } = useLingui();
  const currentLocale = i18n.locale as Locale;
  const [currentZoomLevel, setCurrentZoomLevel] = useState("1");
  const [isMenuOpen, setIsMenuOpen] = useState(false);

  useEffect(() => {
    const saved = localStorage.getItem(zoomLevelStorageKey);
    if (saved) setCurrentZoomLevel(saved);
  }, []);

  const handleLocaleChange = (locale: Locale) => {
    if (locale !== currentLocale) {
      setLocale(locale).then(() => {
        localStorage.setItem(preferredLocaleKey, locale);
      });
    }
  };

  const handleZoomChange = (value: string) => {
    if (value === currentZoomLevel) return;
    if (value === "1") {
      localStorage.removeItem(zoomLevelStorageKey);
    } else {
      localStorage.setItem(zoomLevelStorageKey, value);
    }
    document.documentElement.style.setProperty("--zoom-level", value);
    setCurrentZoomLevel(value);
    window.location.reload();
  };

  const themeIcon =
    theme === "dark" ? (
      <MoonIcon className="size-5" />
    ) : theme === "light" ? (
      <SunIcon className="size-5" />
    ) : resolvedTheme === "dark" ? (
      <MoonStarIcon className="size-5" />
    ) : (
      <SunMoonIcon className="size-5" />
    );

  const triggerClassName = `relative flex h-11 cursor-pointer items-center gap-0 overflow-visible rounded-md border-0 py-2 font-normal text-sm outline-ring hover:bg-hover-background focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 ${
    isCollapsed ? "ml-1.5 w-11 justify-center" : "w-full pr-2 pl-3"
  } ${isMenuOpen ? "bg-hover-background" : ""}`;

  return (
    <div className="relative w-full px-3">
      <DropdownMenu open={isMenuOpen} onOpenChange={setIsMenuOpen}>
        <DropdownMenuTrigger className={triggerClassName} aria-label={t`Preview settings`}>
          <Avatar>
            <AvatarFallback>PP</AvatarFallback>
          </Avatar>
          {!isCollapsed && (
            <>
              <div className="ml-3 flex-1 overflow-hidden text-left font-medium text-ellipsis whitespace-nowrap text-foreground">
                <Trans>PlatformPlatform</Trans>
              </div>
              <ChevronsUpDownIcon className="ml-2 size-3.5 shrink-0 text-foreground opacity-70" />
            </>
          )}
        </DropdownMenuTrigger>
        <DropdownMenuContent
          align="start"
          side={isCollapsed ? "right" : "bottom"}
          className="w-auto bg-popover"
          style={{ minWidth: `${SIDE_MENU_DEFAULT_WIDTH_REM - 1.5}rem` }}
        >
          <FakeProfileCard />

          <DropdownMenuSub>
            <DropdownMenuSubTrigger aria-label={t`Change theme`}>
              {themeIcon}
              <Trans>Theme</Trans>
            </DropdownMenuSubTrigger>
            <DropdownMenuSubContent>
              <DropdownMenuItem onClick={() => setTheme("system")}>
                {resolvedTheme === "dark" ? <MoonStarIcon className="size-5" /> : <SunMoonIcon className="size-5" />}
                <Trans>System</Trans>
                {theme === "system" && <CheckIcon className="ml-auto size-4" />}
              </DropdownMenuItem>
              <DropdownMenuItem onClick={() => setTheme("light")}>
                <SunIcon className="size-5" />
                <Trans>Light</Trans>
                {theme === "light" && <CheckIcon className="ml-auto size-4" />}
              </DropdownMenuItem>
              <DropdownMenuItem onClick={() => setTheme("dark")}>
                <MoonIcon className="size-5" />
                <Trans>Dark</Trans>
                {theme === "dark" && <CheckIcon className="ml-auto size-4" />}
              </DropdownMenuItem>
            </DropdownMenuSubContent>
          </DropdownMenuSub>

          <DropdownMenuSub>
            <DropdownMenuSubTrigger aria-label={t`Change language`}>
              <GlobeIcon className="size-5" />
              <Trans>Language</Trans>
            </DropdownMenuSubTrigger>
            <DropdownMenuSubContent>
              {locales.map((locale) => (
                <DropdownMenuItem key={locale} onClick={() => handleLocaleChange(locale)}>
                  <span>{getLocaleInfo(locale).label}</span>
                  {locale === currentLocale && <CheckIcon className="ml-auto size-4" />}
                </DropdownMenuItem>
              ))}
            </DropdownMenuSubContent>
          </DropdownMenuSub>

          <DropdownMenuSub>
            <DropdownMenuSubTrigger aria-label={t`Change zoom level`}>
              <ZoomInIcon className="size-5" />
              <Trans>Zoom</Trans>
            </DropdownMenuSubTrigger>
            <DropdownMenuSubContent>
              {zoomLevelOptions.map((zoom) => (
                <DropdownMenuItem key={zoom.value} onClick={() => handleZoomChange(zoom.value)}>
                  <span>{zoom.label()}</span>
                  {zoom.value === currentZoomLevel && <CheckIcon className="ml-auto size-4" />}
                </DropdownMenuItem>
              ))}
            </DropdownMenuSubContent>
          </DropdownMenuSub>
        </DropdownMenuContent>
      </DropdownMenu>
    </div>
  );
}
