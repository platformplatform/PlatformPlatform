import { Button } from "@repo/ui/components/Button";
import { Tooltip, TooltipTrigger } from "@repo/ui/components/Tooltip";
import { MoonIcon, MoonStarIcon, SunIcon, SunMoonIcon } from "lucide-react";
import { toggleThemeMode, useThemeMode } from "./mode/ThemeMode";
import { SystemThemeMode, ThemeMode } from "./mode/utils";

/**
 * A button that toggles the theme mode between system, light and dark.
 */
export function ThemeModeSelector({ "aria-label": ariaLabel }: { "aria-label": string }) {
  const { themeMode, resolvedThemeMode, setThemeMode } = useThemeMode();

  const tooltipText = getTooltipText(themeMode, resolvedThemeMode);

  return (
    <TooltipTrigger delay={200}>
      <Button variant="icon" onPress={() => setThemeMode(toggleThemeMode)} aria-label={ariaLabel}>
        <ThemeModeIcon themeMode={themeMode} resolvedThemeMode={resolvedThemeMode} />
      </Button>
      <Tooltip>{tooltipText}</Tooltip>
    </TooltipTrigger>
  );
}

function getTooltipText(themeMode: ThemeMode, resolvedThemeMode: SystemThemeMode): string {
  if (resolvedThemeMode === SystemThemeMode.Dark) {
    return themeMode === ThemeMode.System ? "System mode (dark)" : "Dark mode";
  }
  return themeMode === ThemeMode.System ? "System mode (light)" : "Light mode";
}

function ThemeModeIcon({ themeMode, resolvedThemeMode }: { themeMode: ThemeMode; resolvedThemeMode: SystemThemeMode }) {
  if (resolvedThemeMode === SystemThemeMode.Dark) {
    return themeMode === ThemeMode.System ? <MoonStarIcon className="h-4 w-4" /> : <MoonIcon className="h-4 w-4" />;
  }
  return themeMode === ThemeMode.System ? <SunMoonIcon className="h-4 w-4" /> : <SunIcon className="h-4 w-4" />;
}
