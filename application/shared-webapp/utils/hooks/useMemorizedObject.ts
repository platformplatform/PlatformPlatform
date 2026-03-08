import { useMemo } from "react";

/**
 * Use a memorized object to prevent unnecessary re-renders.
 */
// oxlint-disable-next-line typescript/no-explicit-any -- Any type is supported by JSON.stringify
export function useMemorizedObject<T extends any>(value: T): T {
  const contentHash = JSON.stringify(value);
  // oxlint-disable-next-line react-hooks/exhaustive-deps -- Intentionally memoize by content hash, not by reference
  return useMemo(() => value, [contentHash]);
}
