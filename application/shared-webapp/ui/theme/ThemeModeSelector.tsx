import { MoonIcon, MoonStarIcon, SunIcon, SunMoonIcon } from "lucide-react";
import { Button } from "../components/Button";
import { Tooltip, TooltipContent, TooltipTrigger } from "../components/Tooltip";
import { toggleThemeMode, useTheme } from "./mode/ThemeMode";

/**
 * A button that toggles the theme mode between system, light and dark.
 */
export function ThemeModeSelector({ "aria-label": ariaLabel }: Readonly<{ "aria-label": string }>) {
  const { theme, setTheme, resolvedTheme } = useTheme();

  const tooltipText = getTooltipText(theme, resolvedTheme);

  return (
    <Tooltip>
      <TooltipTrigger
        render={
          <Button
            variant="ghost"
            size="icon-lg"
            onClick={() => setTheme(toggleThemeMode(theme))}
            aria-label={ariaLabel}
          >
            <ThemeModeIcon theme={theme} resolvedTheme={resolvedTheme} />
          </Button>
        }
      />
      <TooltipContent>{tooltipText}</TooltipContent>
    </Tooltip>
  );
}

function getTooltipText(theme: string | undefined, resolvedTheme: string | undefined): string {
  if (resolvedTheme === "dark") {
    return theme === "system" ? "System mode (dark)" : "Dark mode";
  }
  return theme === "system" ? "System mode (light)" : "Light mode";
}

function ThemeModeIcon({ theme, resolvedTheme }: { theme: string | undefined; resolvedTheme: string | undefined }) {
  if (resolvedTheme === "dark") {
    return theme === "system" ? <MoonStarIcon className="size-5" /> : <MoonIcon className="size-5" />;
  }
  return theme === "system" ? <SunMoonIcon className="size-5" /> : <SunIcon className="size-5" />;
}
