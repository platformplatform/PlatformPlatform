import { Button } from "@repo/ui/components/Button";
import { MoonIcon, MoonStarIcon, SunIcon, SunMoonIcon } from "lucide-react";
import { toggleThemeMode, useThemeMode } from "./mode/ThemeMode";

/**
 * A button that toggles the theme mode between system, light and dark.
 */
export function ThemeModeSelector({ "aria-label": ariaLabel }: { "aria-label": string }) {
  const { setThemeMode } = useThemeMode();

  return (
    <Button variant="icon" onPress={() => setThemeMode(toggleThemeMode)} aria-label={ariaLabel}>
      <ThemeModeIcon />
    </Button>
  );
}

function ThemeModeIcon() {
  const { themeMode, resolvedThemeMode } = useThemeMode();

  if (resolvedThemeMode === "dark") {
    return themeMode === "system" ? <MoonStarIcon className="h-4 w-4" /> : <MoonIcon className="h-4 w-4" />;
  }
  return themeMode === "system" ? <SunMoonIcon className="h-4 w-4" /> : <SunIcon className="h-4 w-4" />;
}
