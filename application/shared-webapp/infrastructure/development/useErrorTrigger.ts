import { useEffect, useRef, useState } from "react";

const KONAMI_CODE = [
  "ArrowUp",
  "ArrowUp",
  "ArrowDown",
  "ArrowDown",
  "ArrowLeft",
  "ArrowRight",
  "ArrowLeft",
  "ArrowRight",
  "KeyB",
  "KeyA"
];

export function useErrorTrigger() {
  const [shouldThrowError, setShouldThrowError] = useState(false);
  const sequenceRef = useRef<string[]>([]);

  useEffect(() => {
    const handleKeyDown = (event: KeyboardEvent) => {
      sequenceRef.current.push(event.code);
      sequenceRef.current = sequenceRef.current.slice(-KONAMI_CODE.length);

      if (sequenceRef.current.join(",") === KONAMI_CODE.join(",")) {
        setShouldThrowError(true);
      }
    };

    document.addEventListener("keydown", handleKeyDown);
    return () => document.removeEventListener("keydown", handleKeyDown);
  }, []);

  if (shouldThrowError) {
    throw new Error("Error triggered via Konami code.");
  }
}
