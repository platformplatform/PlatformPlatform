import { useThemeMode } from "./useThemeMode";
import { Button } from "@repo/ui/components/Button";
import { MoonIcon, SunIcon } from "lucide-react";

/**
 * A button that toggles the theme mode between light and dark.
 */
export function ThemeModeSelector() {
  const [themeMode, setThemeMode] = useThemeMode();
  return (
    <Button variant="icon" onPress={() => setThemeMode((themeMode) => (themeMode === "dark" ? "light" : "dark"))}>
      {themeMode === "light" ? <MoonIcon className="w-4 h-4" /> : <SunIcon className="w-4 h-4" />}
    </Button>
  );
}
