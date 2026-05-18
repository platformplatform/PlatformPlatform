import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { SwitchField } from "@repo/ui/components/SwitchField";
import { ToggleGroup, ToggleGroupItem } from "@repo/ui/components/ToggleGroup";
import { useState } from "react";

import { ControlRow } from "./ControlRow";
import { InlineCalendarPreview } from "./InlineCalendarPreview";

export function ControlsPreview() {
  const [showLabels, setShowLabels] = useState(true);
  const [showTooltips, setShowTooltips] = useState(true);
  const [showIcons, setShowIcons] = useState(true);
  const [showValues, setShowValues] = useState(false);
  const [showPlaceholders, setShowPlaceholders] = useState(true);
  const [controlState, setControlState] = useState<"enabled" | "disabled" | "readonly">("enabled");
  const [showErrors, setShowErrors] = useState(false);

  return (
    <>
      <div className="flex flex-wrap gap-x-6 gap-y-2 pb-6">
        <SwitchField label={t`Labels`} checked={showLabels} onCheckedChange={setShowLabels} />
        <SwitchField
          label={t`Tooltips`}
          checked={showTooltips}
          onCheckedChange={setShowTooltips}
          disabled={!showLabels}
        />
        <SwitchField label={t`Icons`} checked={showIcons} onCheckedChange={setShowIcons} />
        <SwitchField label={t`Values`} checked={showValues} onCheckedChange={setShowValues} />
        <SwitchField label={t`Placeholders`} checked={showPlaceholders} onCheckedChange={setShowPlaceholders} />
        <SwitchField label={t`Errors`} checked={showErrors} onCheckedChange={setShowErrors} />
        <ToggleGroup
          variant="outline"
          value={[controlState]}
          onValueChange={(values) => {
            if (values.length > 0) {
              setControlState(values[0] as "enabled" | "disabled" | "readonly");
            }
          }}
        >
          <ToggleGroupItem value="enabled">
            <Trans>Enabled</Trans>
          </ToggleGroupItem>
          <ToggleGroupItem value="readonly">
            <Trans>Read only</Trans>
          </ToggleGroupItem>
          <ToggleGroupItem value="disabled">
            <Trans>Disabled</Trans>
          </ToggleGroupItem>
        </ToggleGroup>
      </div>
      <ControlRow
        suffix="controls"
        label={showLabels}
        tooltip={showTooltips && showLabels}
        showIcon={showIcons}
        values={showValues}
        placeholders={showPlaceholders}
        disabled={controlState === "disabled"}
        readOnly={controlState === "readonly"}
        error={showErrors}
      />
      <div className="mt-8">
        <InlineCalendarPreview
          label={showLabels}
          tooltip={showTooltips && showLabels}
          disabled={controlState === "disabled"}
          readOnly={controlState === "readonly"}
          error={showErrors}
        />
      </div>
    </>
  );
}
