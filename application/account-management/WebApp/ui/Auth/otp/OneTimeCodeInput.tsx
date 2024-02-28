"use client";
import { useCallback, useId, useMemo, useState } from "react";
import { Digit } from "./Digit";
import type { DigitPattern } from "./DigitPattern";

export interface OneTimeCodeInputProps {
  disabled?: boolean;
  length?: number;
  digitPattern?: DigitPattern;
  name?: string;
}

export function OneTimeCodeInput({ digitPattern, disabled, length = 6, name = "code" }: OneTimeCodeInputProps) {
  const [digits, setDigits] = useState(Array(length).fill(""));
  const id = useId();
  const digitRefs = useMemo(() => Array(length).fill(id).map((id, i) => `${id}_${i}`), [id, length]);
  const inputValue = digits.join("");

  const setFocus = useCallback((i: number) => {
    if (i >= digitRefs.length) {
      const el = document.querySelectorAll("button[type=submit]")[0] as HTMLInputElement | null;
      el?.focus();
    }
    else {
      const el = document.getElementById(digitRefs[i]) as HTMLInputElement | null;
      el?.focus();
    }
  }, [digitRefs]);

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
    const nextFocusIndex = value.length > 0
      ? Math.min(digits.length, i + 1)
      : Math.max(0, i - 1);
    setFocus(nextFocusIndex);
  };
  return (
    <div className="flex flex-row gap-4">
      {digits.map((digit, i) => (
        <Digit
          key={i}
          id={digitRefs[i]}
          tabIndex={i + 1}
          value={digit}
          digitPattern={digitPattern}
          autoComplete={i === 0 ? "one-time-code" : undefined}
          onChange={value => onChangeHandler(value, i)}
          autoFocus={i === inputValue.length}
          disabled={disabled}
        />
      ))}
      <input type="hidden" name={name} value={inputValue} />
    </div>
  );
}
