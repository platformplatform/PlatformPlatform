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

export function ThemeModeSelector() {
  const { theme, setTheme, resolvedTheme } = useTheme();

  const getThemeIcon = () => {
    if (theme === ThemeMode.Dark || (theme === ThemeMode.System && resolvedTheme === ThemeMode.Dark)) {
      return theme === ThemeMode.System ? <MoonStarIcon className="size-5" /> : <MoonIcon className="size-5" />;
    }
    return theme === ThemeMode.System ? <SunMoonIcon className="size-5" /> : <SunIcon className="size-5" />;
  };

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
        <DropdownMenuItem onClick={() => setTheme(ThemeMode.System)}>
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
        <DropdownMenuItem onClick={() => setTheme(ThemeMode.Light)}>
          <div className="flex items-center gap-2">
            <SunIcon className="size-5" />
            <Trans>Light</Trans>
            {theme === ThemeMode.Light && <CheckIcon className="ml-auto size-5" />}
          </div>
        </DropdownMenuItem>
        <DropdownMenuItem onClick={() => setTheme(ThemeMode.Dark)}>
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
