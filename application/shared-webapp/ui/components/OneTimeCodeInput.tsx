import { forwardRef, useCallback, useId, useImperativeHandle, useMemo, useState } from "react";
import type { DigitPattern } from "./Digit";
import { Digit } from "./Digit";

export interface OneTimeCodeInputProps {
  disabled?: boolean;
  length?: number;
  digitPattern?: DigitPattern;
  name?: string;
  autoFocus?: boolean;
  ariaLabel: string;
  onValueChange?: (value: string, isComplete: boolean) => void;
}

export interface OneTimeCodeInputRef {
  reset: () => void;
  focus: () => void;
  getValue: () => string;
  isComplete: () => boolean;
}

export const OneTimeCodeInput = forwardRef<OneTimeCodeInputRef, OneTimeCodeInputProps>(function OneTimeCodeInput(
  { digitPattern, disabled, length = 6, name = "code", autoFocus, ariaLabel, onValueChange },
  ref
) {
  const [digits, setDigits] = useState<string[]>(new Array(length).fill(""));
  const id = useId();
  const digitRefs = useMemo(() => new Array(length).fill(id).map((id, i) => `${id}_${i}`), [id, length]);
  const inputValue = digits.join("");
  const isComplete = inputValue.length === length;

  const setFocus = useCallback(
    (i: number) => {
      const formEl = (document.getElementById(digitRefs[0]) as HTMLInputElement | null)?.form as HTMLFormElement | null;
      if (formEl === null) {
        return;
      }

      if (i >= digitRefs.length) {
        const el = formEl.querySelectorAll("button[type=submit]")[0] as HTMLInputElement | null;
        el?.focus();
      } else {
        const el = document.getElementById(digitRefs[i]) as HTMLInputElement | null;
        el?.focus();
      }
    },
    [digitRefs]
  );

  useImperativeHandle(
    ref,
    () => ({
      reset: () => {
        setDigits(new Array(length).fill(""));
        setFocus(0);
      },
      focus: () => {
        setFocus(0);
      },
      getValue: () => inputValue,
      isComplete: () => isComplete
    }),
    [inputValue, isComplete, length, setFocus]
  );

  const onChangeHandler = (value: string, i: number): void => {
    let newDigits: string[];

    if (value.length > 1) {
      // If the user pastes more than one digit
      const pastedDigits = value.substring(0, length).split("");
      newDigits = [...pastedDigits];
      setDigits(newDigits);
      setFocus(pastedDigits.length);
    } else {
      newDigits = [...digits];
      newDigits[i] = value;
      setDigits(newDigits);
      const nextFocusIndex = value.length > 0 ? Math.min(digits.length, i + 1) : Math.max(0, i - 1);
      setFocus(nextFocusIndex);
    }

    // Call onValueChange callback if provided
    const newValue = newDigits.join("");
    const newIsComplete = newValue.length === length;
    onValueChange?.(newValue, newIsComplete);
  };
  return (
    <fieldset className="flex flex-row gap-4 border-0 p-0" aria-label={ariaLabel}>
      {digits.map((digit, i) => (
        <Digit
          // biome-ignore lint/suspicious/noArrayIndexKey: The index is used as a unique key for the digit
          key={i}
          id={digitRefs[i]}
          value={digit}
          digitPattern={digitPattern}
          autoComplete={i === 0 ? "one-time-code" : undefined}
          onChange={(value) => onChangeHandler(value, i)}
          autoFocus={i === inputValue.length && autoFocus}
          disabled={disabled}
        />
      ))}
      <input type="hidden" name={name} value={inputValue} />
    </fieldset>
  );
});
