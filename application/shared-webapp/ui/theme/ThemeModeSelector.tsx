import { toggleThemeMode, useThemeMode } from "./mode/ThemeMode";
import { Button } from "@repo/ui/components/Button";
import { MoonIcon, MoonStarIcon, SunIcon, SunMoonIcon } from "lucide-react";

/**
 * A button that toggles the theme mode between light and dark.
 */
export function ThemeModeSelector() {
  const { setThemeMode } = useThemeMode();
  return (
    <Button variant="icon" onPress={() => setThemeMode(toggleThemeMode)}>
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
