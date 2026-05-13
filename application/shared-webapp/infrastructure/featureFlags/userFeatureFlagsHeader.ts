/**
 * Bridge between the fetch layer (no React) and AuthenticationProvider (React).
 *
 * AppGateway emits `x-user-feature-flags` on every authenticated response with the comma-separated
 * keys of database-scoped flags currently enabled for the user. Fetch interceptors call
 * `dispatchUserFeatureFlagsFromResponse(response)`; AuthenticationProvider subscribes on mount and
 * diffs the keys against its state, updating only on change.
 */

export const USER_FEATURE_FLAGS_HEADER = "x-user-feature-flags";

type Listener = (flagKeys: string[]) => void;

const listeners = new Set<Listener>();

function parseHeader(value: string): string[] {
  return value
    .split(",")
    .map((key) => key.trim())
    .filter((key) => key.length > 0);
}

export function dispatchUserFeatureFlagsFromResponse(response: Response): void {
  const headerValue = response.headers.get(USER_FEATURE_FLAGS_HEADER);
  if (headerValue === null) return;

  const flagKeys = parseHeader(headerValue);
  for (const listener of listeners) {
    listener(flagKeys);
  }
}

export function subscribeToUserFeatureFlags(listener: Listener): () => void {
  listeners.add(listener);
  return () => {
    listeners.delete(listener);
  };
}
