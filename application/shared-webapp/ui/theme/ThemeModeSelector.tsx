import type { Key } from "@react-types/shared";
import { Button } from "@repo/ui/components/Button";
import { Menu, MenuItem, MenuTrigger } from "@repo/ui/components/Menu";
import { Tooltip, TooltipTrigger } from "@repo/ui/components/Tooltip";
import { CheckIcon, MoonIcon, MoonStarIcon, SunIcon, SunMoonIcon } from "lucide-react";
import { useThemeMode } from "./mode/ThemeMode";
import { useSystemThemeMode } from "./mode/useSystemThemeMode";
import { SystemThemeMode, ThemeMode } from "./mode/utils";

/**
 * A button that opens a menu to select theme mode between system, light and dark.
 */
export function ThemeModeSelector({
  "aria-label": ariaLabel,
  tooltip,
  variant = "icon",
  onAction
}: {
  readonly "aria-label": string;
  readonly tooltip?: string;
  readonly variant?: "icon" | "mobile-menu";
  readonly onAction?: () => void;
}) {
  const { themeMode, setThemeMode } = useThemeMode();

  const handleThemeChange = (key: Key) => {
    setThemeMode(key as ThemeMode);
    onAction?.();
  };

  const getThemeName = (mode: ThemeMode) => {
    switch (mode) {
      case ThemeMode.System:
        return "System";
      case ThemeMode.Light:
        return "Light";
      case ThemeMode.Dark:
        return "Dark";
      default:
        return "System";
    }
  };

  const menuContent = (
    <MenuTrigger>
      {variant === "icon" ? (
        <Button variant="icon" aria-label={ariaLabel}>
          <ThemeModeIcon themeMode={themeMode} />
        </Button>
      ) : (
        <Button
          variant="ghost"
          className="flex h-11 w-full items-center justify-start gap-4 px-3 py-2 font-normal text-base text-muted-foreground hover:bg-hover-background hover:text-foreground"
          style={{ pointerEvents: "auto" }}
        >
          <div className="flex h-6 w-6 shrink-0 items-center justify-center">
            <ThemeModeIcon themeMode={themeMode} />
          </div>
          <div className="min-w-0 flex-1 overflow-hidden whitespace-nowrap text-start">Theme</div>
          <div className="shrink-0 text-base text-muted-foreground">{getThemeName(themeMode)}</div>
        </Button>
      )}
      <Menu onAction={handleThemeChange} aria-label={ariaLabel} placement="bottom end">
        <MenuItem id={ThemeMode.System} textValue="System">
          <div className="flex items-center gap-2">
            <SystemThemeIcon className="h-4 w-4" />
            <span>System</span>
            {themeMode === ThemeMode.System && <CheckIcon className="ml-auto h-4 w-4" />}
          </div>
        </MenuItem>
        <MenuItem id={ThemeMode.Light} textValue="Light">
          <div className="flex items-center gap-2">
            <SunIcon className="h-4 w-4" />
            <span>Light</span>
            {themeMode === ThemeMode.Light && <CheckIcon className="ml-auto h-4 w-4" />}
          </div>
        </MenuItem>
        <MenuItem id={ThemeMode.Dark} textValue="Dark">
          <div className="flex items-center gap-2">
            <MoonIcon className="h-4 w-4" />
            <span>Dark</span>
            {themeMode === ThemeMode.Dark && <CheckIcon className="ml-auto h-4 w-4" />}
          </div>
        </MenuItem>
      </Menu>
    </MenuTrigger>
  );

  if (tooltip) {
    return (
      <TooltipTrigger>
        {menuContent}
        <Tooltip>{tooltip}</Tooltip>
      </TooltipTrigger>
    );
  }

  return menuContent;
}

// Component that always shows the system theme icon
function SystemThemeIcon({ className }: { className: string }) {
  const systemTheme = useSystemThemeMode();

  return systemTheme === SystemThemeMode.Dark ? (
    <MoonStarIcon className={className} />
  ) : (
    <SunMoonIcon className={className} />
  );
}

function ThemeModeIcon({ themeMode }: Readonly<{ themeMode: ThemeMode }>) {
  // For System mode, show special icons based on resolved theme
  if (themeMode === ThemeMode.System) {
    return <SystemThemeIcon className="h-5 w-5" />;
  }

  // For explicit Light/Dark modes, show icons based on the selected mode
  return themeMode === ThemeMode.Dark ? <MoonIcon className="h-5 w-5" /> : <SunIcon className="h-5 w-5" />;
}
