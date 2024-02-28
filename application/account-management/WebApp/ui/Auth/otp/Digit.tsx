import { useMemo } from "react";
import { DigitPattern } from "./DigitPattern";

export interface DigitProps {
  value: string;
  onChange: (value: string) => void;
  autoFocus?: boolean;
  tabIndex?: number;
  id?: string;
  disabled?: boolean;
  autoComplete?: string;
  digitPattern?: DigitPattern;
}

export function Digit({
  id,
  value,
  onChange,
  autoFocus,
  tabIndex,
  disabled,
  autoComplete,
  digitPattern = DigitPattern.Digits,
  ...props
}: DigitProps) {
  const inputPattern = useMemo(() => `${digitPattern}*`, [digitPattern]);
  const isCharValid = useMemo(() => new RegExp(`^${digitPattern}$`), [digitPattern]);
  const isStringValid = useMemo(() => new RegExp(`^${digitPattern}+$`), [digitPattern]);

  return (
    <input
      {...props}
      id={id}
      tabIndex={tabIndex}
      type="text"
      inputMode="numeric"
      pattern={inputPattern}
      maxLength={1}
      value={value}
      onChange={() => {}}
      onKeyUp={(e) => {
        if (e.key === "Backspace")
          onChange("");
        else if (isCharValid.test(e.key))
          onChange(e.key);
      }}
      onPaste={(e) => {
        e.preventDefault();
        const text = e.clipboardData.getData("text");
        if (isStringValid.test(text))
          onChange(text);
      }}
      autoComplete={autoComplete}
      className="h-14 w-10 rounded border border-gray-400 bg-transparent text-center text-black dark:text-white"
      autoFocus={autoFocus}
      disabled={disabled}
    />
  );
}
