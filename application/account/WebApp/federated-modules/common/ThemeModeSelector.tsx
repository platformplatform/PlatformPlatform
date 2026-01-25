import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Button } from "@repo/ui/components/Button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger
} from "@repo/ui/components/DropdownMenu";
import { Tooltip, TooltipContent, TooltipTrigger } from "@repo/ui/components/Tooltip";
import { CheckIcon, MoonIcon, MoonStarIcon, SunIcon, SunMoonIcon } from "lucide-react";
import { useTheme } from "next-themes";

const ThemeMode = {
  System: "system",
  Light: "light",
  Dark: "dark"
} as const;

export default function ThemeModeSelector({
  variant = "icon",
  onAction
}: {
  variant?: "icon" | "mobile-menu";
  onAction?: () => void;
} = {}) {
  const { theme, setTheme, resolvedTheme } = useTheme();

  const handleThemeChange = (newTheme: string) => {
    setTheme(newTheme);
    onAction?.();
  };

  const getThemeIcon = () => {
    if (theme === ThemeMode.Dark || (theme === ThemeMode.System && resolvedTheme === ThemeMode.Dark)) {
      return theme === ThemeMode.System ? <MoonStarIcon className="size-5" /> : <MoonIcon className="size-5" />;
    }
    return theme === ThemeMode.System ? <SunMoonIcon className="size-5" /> : <SunIcon className="size-5" />;
  };

  const getThemeLabel = () => {
    if (theme === ThemeMode.System) {
      return <Trans>System</Trans>;
    }
    if (theme === ThemeMode.Light) {
      return <Trans>Light</Trans>;
    }
    return <Trans>Dark</Trans>;
  };

  if (variant === "mobile-menu") {
    return (
      <DropdownMenu>
        <DropdownMenuTrigger
          render={
            <Button
              variant="ghost"
              className="flex h-11 w-full items-center justify-start gap-4 px-3 py-2 font-normal text-base text-muted-foreground hover:bg-hover-background hover:text-foreground"
              style={{ pointerEvents: "auto" }}
            >
              <div className="flex size-6 shrink-0 items-center justify-center">{getThemeIcon()}</div>
              <div className="min-w-0 flex-1 overflow-hidden whitespace-nowrap text-start">
                <Trans>Theme</Trans>
              </div>
              <div className="shrink-0 text-base text-muted-foreground">{getThemeLabel()}</div>
            </Button>
          }
        />
        <DropdownMenuContent align="end">
          <DropdownMenuItem onClick={() => handleThemeChange(ThemeMode.System)}>
            <div className="flex items-center gap-2">
              {resolvedTheme === ThemeMode.Dark ? (
                <MoonStarIcon className="size-5" />
              ) : (
                <SunMoonIcon className="size-5" />
              )}
              <Trans>System</Trans>
              {theme === ThemeMode.System && <CheckIcon className="ml-auto size-5" />}
            </div>
          </DropdownMenuItem>
          <DropdownMenuItem onClick={() => handleThemeChange(ThemeMode.Light)}>
            <div className="flex items-center gap-2">
              <SunIcon className="size-5" />
              <Trans>Light</Trans>
              {theme === ThemeMode.Light && <CheckIcon className="ml-auto size-5" />}
            </div>
          </DropdownMenuItem>
          <DropdownMenuItem onClick={() => handleThemeChange(ThemeMode.Dark)}>
            <div className="flex items-center gap-2">
              <MoonIcon className="size-5" />
              <Trans>Dark</Trans>
              {theme === ThemeMode.Dark && <CheckIcon className="ml-auto size-5" />}
            </div>
          </DropdownMenuItem>
        </DropdownMenuContent>
      </DropdownMenu>
    );
  }

  return (
    <DropdownMenu>
      <Tooltip>
        <TooltipTrigger
          render={
            <DropdownMenuTrigger
              render={
                <Button variant="ghost" size="icon" aria-label={t`Change theme`}>
                  {getThemeIcon()}
                </Button>
              }
            />
          }
        />
        <TooltipContent>{t`Change theme`}</TooltipContent>
      </Tooltip>
      <DropdownMenuContent align="start">
        <DropdownMenuItem onClick={() => handleThemeChange(ThemeMode.System)}>
          <div className="flex items-center gap-2">
            {resolvedTheme === ThemeMode.Dark ? (
              <MoonStarIcon className="size-5" />
            ) : (
              <SunMoonIcon className="size-5" />
            )}
            <Trans>System</Trans>
            {theme === ThemeMode.System && <CheckIcon className="ml-auto size-5" />}
          </div>
        </DropdownMenuItem>
        <DropdownMenuItem onClick={() => handleThemeChange(ThemeMode.Light)}>
          <div className="flex items-center gap-2">
            <SunIcon className="size-5" />
            <Trans>Light</Trans>
            {theme === ThemeMode.Light && <CheckIcon className="ml-auto size-5" />}
          </div>
        </DropdownMenuItem>
        <DropdownMenuItem onClick={() => handleThemeChange(ThemeMode.Dark)}>
          <div className="flex items-center gap-2">
            <MoonIcon className="size-5" />
            <Trans>Dark</Trans>
            {theme === ThemeMode.Dark && <CheckIcon className="ml-auto size-5" />}
          </div>
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
