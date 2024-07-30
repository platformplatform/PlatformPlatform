import { useMemo } from "react";

/**
 * Use a memorized object to prevent unnecessary re-renders.
 */
// biome-ignore lint/complexity/noUselessTypeConstraint: Any type is supported by JSON.stringify
export function useMemorizedObject<T extends any>(value: T): T {
  const contentHash = JSON.stringify(value);
  // biome-ignore lint/correctness/useExhaustiveDependencies: We use the content hash to detect changes
  return useMemo(() => value, [contentHash]);
}
