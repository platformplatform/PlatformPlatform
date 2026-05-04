import { t } from "@lingui/core/macro";
import { TextAreaField } from "@repo/ui/components/TextAreaField";

import type { ControlRowDerivedProps } from "./controlRowTypes";

import { tooltips } from "./controlTooltips";

export function TextAreaFields({
  suffix,
  label,
  tooltip,
  disabled,
  readOnly,
  hasValues,
  placeholders,
  errorMessage
}: ControlRowDerivedProps) {
  return (
    <>
      <TextAreaField
        label={label ? t`Text area` : undefined}
        tooltip={tooltip ? tooltips.textArea : undefined}
        name={`textarea-${suffix}`}
        placeholder={placeholders ? t`Add notes here` : undefined}
        defaultValue={hasValues ? t`Meeting notes from last week` : undefined}
        disabled={disabled}
        readOnly={readOnly}
        errorMessage={errorMessage}
      />
      <TextAreaField
        label={label ? t`Address (fixed 2 lines)` : undefined}
        tooltip={tooltip ? tooltips.textAreaFixed : undefined}
        name={`textarea-fixed-${suffix}`}
        placeholder={placeholders ? t`Street address` : undefined}
        defaultValue={hasValues ? t`123 Example Street\nAnytown, AA 12345` : undefined}
        lines={2}
        resizable={false}
        disabled={disabled}
        readOnly={readOnly}
        errorMessage={errorMessage}
      />
    </>
  );
}
