import { useMemo } from "react";
import { useFocusRing } from "react-aria";
import { tv } from "tailwind-variants";
import { focusRing } from "./focusRing";

const digitStyles = tv({
  extend: focusRing,
  base: "h-14 w-10 rounded-md border border-input bg-transparent text-center text-input-foreground"
});

export enum DigitPattern {
  Digits = "[0-9]",
  Chars = "[A-Za-z]",
  DigitsAndChars = "[0-9A-Za-z]"
}

export interface DigitProps {
  value: string;
  onChange: (value: string) => void;
  autoFocus?: boolean;
  tabIndex?: number;
  id?: string;
  disabled?: boolean;
  autoComplete?: string;
  digitPattern?: DigitPattern;
  className?: string;
}

export function Digit({
  id,
  value,
  onChange,
  autoFocus,
  tabIndex,
  disabled,
  autoComplete,
  className,
  digitPattern = DigitPattern.Digits,
  ...props
}: DigitProps) {
  const { focusProps, isFocusVisible } = useFocusRing();
  const inputPattern = useMemo(() => `${digitPattern}*`, [digitPattern]);
  const isCharValid = useMemo(() => new RegExp(`^${digitPattern}$`), [digitPattern]);
  const isStringValid = useMemo(() => new RegExp(`^${digitPattern}+$`), [digitPattern]);

  return (
    <input
      {...props}
      {...focusProps}
      id={id}
      tabIndex={tabIndex}
      type="text"
      inputMode={digitPattern === DigitPattern.Digits ? "numeric" : "text"}
      pattern={inputPattern}
      maxLength={1}
      value={value}
      onChange={() => {}}
      onKeyUp={(e) => {
        if (e.key === "Backspace") {
          onChange("");
        } else if (isCharValid.test(e.key)) {
          onChange(e.key);
        }
      }}
      onPaste={(e) => {
        e.preventDefault();
        const text = e.clipboardData.getData("text");
        if (isStringValid.test(text)) {
          onChange(text);
        }
      }}
      autoComplete={autoComplete}
      className={digitStyles({ className, isFocusVisible })}
      // biome-ignore lint/a11y/noAutofocus: The autofocus attribute is used to focus the first digit input
      autoFocus={autoFocus}
      disabled={disabled}
    />
  );
}
