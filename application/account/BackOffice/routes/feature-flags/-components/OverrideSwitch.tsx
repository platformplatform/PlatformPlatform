import { Trans } from "@lingui/react/macro";
import { Switch } from "@repo/ui/components/Switch";
import { Tooltip, TooltipContent, TooltipTrigger } from "@repo/ui/components/Tooltip";

interface OverrideSwitchProps {
  isManualOverride: boolean;
  checked: boolean;
  onCheckedChange: (checked: boolean) => void;
  disabled: boolean;
  dimmed: boolean;
  ariaLabel: string;
}

// Shared switch for feature-flag override toggling. Renders a warning-colored switch wrapped in a tooltip
// when the row currently has a manual override (signals "this row deviates from the default and a third
// click will clear the override"); otherwise renders the plain switch.
export function OverrideSwitch({
  isManualOverride,
  checked,
  onCheckedChange,
  disabled,
  dimmed,
  ariaLabel
}: Readonly<OverrideSwitchProps>) {
  const dimClass = dimmed ? "opacity-50" : "";
  if (!isManualOverride) {
    return (
      <Switch
        checked={checked}
        onCheckedChange={onCheckedChange}
        disabled={disabled}
        className={dimClass}
        aria-label={ariaLabel}
      />
    );
  }
  return (
    <Tooltip>
      <TooltipTrigger
        render={
          <Switch
            checked={checked}
            onCheckedChange={onCheckedChange}
            disabled={disabled}
            className={`data-checked:bg-warning data-unchecked:bg-warning dark:data-unchecked:bg-warning ${dimClass}`}
            aria-label={ariaLabel}
          />
        }
      />
      <TooltipContent>
        <Trans>Manual override active. Click again to flip, three times to clear.</Trans>
      </TooltipContent>
    </Tooltip>
  );
}
