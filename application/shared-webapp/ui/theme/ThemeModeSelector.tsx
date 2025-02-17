import { Button } from "@repo/ui/components/Button";
import { MoonIcon, MoonStarIcon, SunIcon, SunMoonIcon } from "lucide-react";
import { toggleThemeMode, useThemeMode } from "./mode/ThemeMode";

/**
 * A button that toggles the theme mode between light and dark.
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
  if (themeMode === "light") {
    return <SunIcon className="w-4 h-4" />;
  }
  if (themeMode === "dark") {
    return <MoonIcon className="w-4 h-4" />;
  }
  if (resolvedThemeMode === "light") {
    return <SunMoonIcon className="w-4 h-4" />;
  }
  return <MoonStarIcon className="w-4 h-4" />;
}
