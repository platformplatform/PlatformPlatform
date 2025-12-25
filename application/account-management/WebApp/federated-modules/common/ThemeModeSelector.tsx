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
    if (theme === "dark" || (theme === "system" && resolvedTheme === "dark")) {
      return theme === "system" ? <MoonStarIcon className="size-5" /> : <MoonIcon className="size-5" />;
    }
    return theme === "system" ? <SunMoonIcon className="size-5" /> : <SunIcon className="size-5" />;
  };

  const getThemeLabel = () => {
    if (theme === "system") {
      return <Trans>System</Trans>;
    }
    if (theme === "light") {
      return <Trans>Light</Trans>;
    }
    return <Trans>Dark</Trans>;
  };

  const menuContent = (
    <DropdownMenu>
      <DropdownMenuTrigger
        render={
          variant === "icon" ? (
            <Button variant="ghost" size="icon-lg" aria-label={t`Change theme`}>
              {getThemeIcon()}
            </Button>
          ) : (
            <Button
              variant="ghost"
              className="flex h-11 w-full items-center justify-start gap-4 px-3 py-2 font-normal text-base text-muted-foreground hover:bg-hover-background hover:text-foreground"
              style={{ pointerEvents: "auto" }}
            >
              <div className="flex h-6 w-6 shrink-0 items-center justify-center">{getThemeIcon()}</div>
              <div className="min-w-0 flex-1 overflow-hidden whitespace-nowrap text-start">
                <Trans>Theme</Trans>
              </div>
              <div className="shrink-0 text-base text-muted-foreground">{getThemeLabel()}</div>
            </Button>
          )
        }
      />
      <DropdownMenuContent align={variant === "mobile-menu" ? "end" : "start"} className="w-auto">
        <DropdownMenuItem onClick={() => handleThemeChange("system")}>
          <div className="flex items-center gap-2">
            {resolvedTheme === "dark" ? <MoonStarIcon className="h-5 w-5" /> : <SunMoonIcon className="h-5 w-5" />}
            <Trans>System</Trans>
            {theme === "system" && <CheckIcon className="ml-auto h-5 w-5" />}
          </div>
        </DropdownMenuItem>
        <DropdownMenuItem onClick={() => handleThemeChange("light")}>
          <div className="flex items-center gap-2">
            <SunIcon className="h-5 w-5" />
            <Trans>Light</Trans>
            {theme === "light" && <CheckIcon className="ml-auto h-5 w-5" />}
          </div>
        </DropdownMenuItem>
        <DropdownMenuItem onClick={() => handleThemeChange("dark")}>
          <div className="flex items-center gap-2">
            <MoonIcon className="h-5 w-5" />
            <Trans>Dark</Trans>
            {theme === "dark" && <CheckIcon className="ml-auto h-5 w-5" />}
          </div>
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  );

  return (
    <Tooltip>
      <TooltipTrigger render={menuContent} />
      <TooltipContent>{t`Change theme`}</TooltipContent>
    </Tooltip>
  );
}
