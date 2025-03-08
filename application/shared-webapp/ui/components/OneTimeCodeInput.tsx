import { useCallback, useId, useMemo, useState } from "react";
import { Digit } from "./Digit";
import type { DigitPattern } from "./Digit";

export interface OneTimeCodeInputProps {
  disabled?: boolean;
  length?: number;
  digitPattern?: DigitPattern;
  name?: string;
  autoFocus?: boolean;
  ariaLabel: string;
}

export function OneTimeCodeInput({
  digitPattern,
  disabled,
  length = 6,
  name = "code",
  autoFocus,
  ariaLabel
}: OneTimeCodeInputProps) {
  const [digits, setDigits] = useState<string[]>(new Array(length).fill(""));
  const id = useId();
  const digitRefs = useMemo(() => new Array(length).fill(id).map((id, i) => `${id}_${i}`), [id, length]);
  const inputValue = digits.join("");

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

  const onChangeHandler = (value: string, i: number): void => {
    if (value.length > 1) {
      // If the user pastes more than one digit
      const pastedDigits = value.substring(0, length).split("");
      setDigits([...pastedDigits]);
      setFocus(pastedDigits.length);
      return;
    }
    const newDigits = [...digits];
    newDigits[i] = value;
    setDigits(newDigits);
    const nextFocusIndex = value.length > 0 ? Math.min(digits.length, i + 1) : Math.max(0, i - 1);
    setFocus(nextFocusIndex);
  };
  return (
    <div className="flex flex-row gap-4" aria-label={ariaLabel}>
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
    </div>
  );
}
