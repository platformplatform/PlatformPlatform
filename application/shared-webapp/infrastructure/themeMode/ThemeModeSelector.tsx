import { useThemeMode } from "./useThemeMode";
import { Button } from "@repo/ui/components/Button";
import { MoonIcon, SunIcon } from "lucide-react";

/**
 * A button that toggles the theme mode between light and dark.
 */
export function ThemeModeSelector() {
  const [themeMode, setThemeMode] = useThemeMode();
  return (
    <Button variant="icon">
      {themeMode === "light" ? (
        <MoonIcon onClick={() => setThemeMode("dark")} />
      ) : (
        <SunIcon onClick={() => setThemeMode("light")} />
      )}
    </Button>
  );
}
