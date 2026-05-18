import { t } from "@lingui/core/macro";
import { InputOtpField } from "@repo/ui/components/InputOtpField";
import { NumberField } from "@repo/ui/components/NumberField";
import { SliderField } from "@repo/ui/components/SliderField";
import { TextField } from "@repo/ui/components/TextField";
import { EuroIcon, HashIcon, SearchIcon } from "lucide-react";
import { useState } from "react";

import type { ControlRowProps } from "./controlRowTypes";

import { tooltips } from "./controlTooltips";
import { DateAndToggleFields } from "./DateAndToggleFields";
import { SelectAndComboboxFields } from "./SelectAndComboboxFields";

export function ControlRow({
  suffix,
  label,
  tooltip,
  disabled,
  readOnly,
  error,
  showIcon,
  values,
  placeholders
}: ControlRowProps) {
  const hasValues = !!values;
  const errorMessage = error ? t`This field is required` : undefined;
  const [otpValue, setOtpValue] = useState(hasValues ? "123456" : "");
  const derived = {
    suffix,
    label,
    tooltip,
    disabled,
    readOnly,
    error,
    showIcon,
    hasValues,
    placeholders,
    errorMessage
  };

  return (
    <div
      key={String(hasValues)}
      className="grid grid-cols-2 gap-x-6 gap-y-4 md:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5 2xl:grid-cols-6"
    >
      <TextField
        label={label ? t`Text field` : undefined}
        tooltip={tooltip ? tooltips.textField : undefined}
        name={`text-${suffix}`}
        placeholder={placeholders ? t`E.g., Alex Taylor` : undefined}
        defaultValue={hasValues ? t`Alex Taylor` : undefined}
        disabled={disabled}
        readOnly={readOnly}
        errorMessage={errorMessage}
        startIcon={showIcon ? <SearchIcon /> : undefined}
      />
      <NumberField
        label={label ? t`Number (integer)` : undefined}
        tooltip={tooltip ? tooltips.numberInteger : undefined}
        name={`integer-${suffix}`}
        defaultValue={hasValues ? 42 : undefined}
        minValue={0}
        maxValue={100}
        step={1}
        allowEmpty
        disabled={disabled}
        readOnly={readOnly}
        errorMessage={errorMessage}
        startIcon={showIcon ? <HashIcon /> : undefined}
      />
      <NumberField
        label={label ? t`Number (decimal)` : undefined}
        tooltip={tooltip ? tooltips.numberDecimal : undefined}
        name={`decimal-${suffix}`}
        defaultValue={hasValues ? 149.95 : undefined}
        minValue={0}
        maxValue={999.99}
        step={0.1}
        decimalPlaces={2}
        allowEmpty
        disabled={disabled}
        readOnly={readOnly}
        errorMessage={errorMessage}
        startIcon={showIcon ? <EuroIcon /> : undefined}
      />
      <SelectAndComboboxFields {...derived} />
      <DateAndToggleFields {...derived} />
      <SliderField
        name={`slider-steps-${suffix}`}
        label={label ? t`Slider with steps` : undefined}
        tooltip={tooltip ? tooltips.slider : undefined}
        disabled={disabled}
        readOnly={readOnly}
        errorMessage={errorMessage}
        defaultValue={hasValues ? [60] : [40]}
        step={20}
      />
      <SliderField
        name={`slider-range-${suffix}`}
        label={label ? t`Slider (range)` : undefined}
        tooltip={tooltip ? tooltips.sliderRange : undefined}
        disabled={disabled}
        readOnly={readOnly}
        errorMessage={errorMessage}
        defaultValue={hasValues ? [25, 75] : [20, 80]}
      />
      <InputOtpField
        label={label ? t`One-time code` : undefined}
        tooltip={tooltip ? tooltips.inputOtp : undefined}
        name={`otp-${suffix}`}
        value={otpValue}
        onChange={setOtpValue}
        disabled={disabled}
        readOnly={readOnly}
        errorMessage={errorMessage}
      />
    </div>
  );
}
