/**
 * Bridge between the fetch layer (no React) and AuthenticationProvider (React).
 *
 * AppGateway emits `x-user-feature-flags` on every authenticated response with the comma-separated
 * keys of database-scoped flags currently enabled for the user. Fetch interceptors call
 * `dispatchUserFeatureFlagsFromResponse(response)`; AuthenticationProvider subscribes on mount.
 *
 * The listener Set is stored on globalThis under a Symbol.for key so every federated bundle that
 * imports this module shares one Set. Without this, each bundle's module instance owns its own Set
 * and dispatches do not cross the host/remote boundary (e.g., a flag toggled from the Account
 * remote never updates main-host `useFeatureFlag` consumers).
 *
 * Contract:
 * - Header missing on a response = no dispatch (preserves prior state).
 * - Header present with empty value = dispatch `[]` (user has zero enabled flags now).
 * - Parsed flag keys are deduplicated and sorted so equality checks downstream are stable.
 * - Late subscribers receive the most recently dispatched value immediately so a fetch that
 *   resolves before AuthenticationProvider's useEffect runs is not silently dropped.
 */

export const USER_FEATURE_FLAGS_HEADER = "x-user-feature-flags";

type Listener = (flagKeys: string[]) => void;

type Registry = {
  listeners: Set<Listener>;
  lastFlagKeys: string[] | null;
};

const REGISTRY_KEY = Symbol.for("@repo/infrastructure/userFeatureFlagsHeader");

function getRegistry(): Registry {
  const globalRegistry = globalThis as unknown as { [REGISTRY_KEY]?: Registry };
  if (!globalRegistry[REGISTRY_KEY]) {
    globalRegistry[REGISTRY_KEY] = { listeners: new Set(), lastFlagKeys: null };
  }
  return globalRegistry[REGISTRY_KEY];
}

function parseHeader(value: string): string[] {
  const seen = new Set<string>();
  for (const segment of value.split(",")) {
    const key = segment.trim();
    if (key.length > 0) seen.add(key);
  }
  return [...seen].sort();
}

export function dispatchUserFeatureFlagsFromResponse(response: Response): void {
  const headerValue = response.headers.get(USER_FEATURE_FLAGS_HEADER);
  if (headerValue === null) return;

  const flagKeys = parseHeader(headerValue);
  const registry = getRegistry();
  registry.lastFlagKeys = flagKeys;
  for (const listener of registry.listeners) {
    listener(flagKeys);
  }
}

export function subscribeToUserFeatureFlags(listener: Listener): () => void {
  const registry = getRegistry();
  registry.listeners.add(listener);
  if (registry.lastFlagKeys !== null) {
    listener(registry.lastFlagKeys);
  }
  return () => {
    registry.listeners.delete(listener);
  };
}

/**
 * Synchronous read of the most recent header-dispatched flag set. Returns null when no header has
 * been seen yet (no authenticated response has resolved in this session). Used by non-React
 * callers — TanStack Router `beforeLoad` guards, fetch interceptors — so they share the live
 * eventing channel instead of reading a bootstrap meta tag that goes stale after the first
 * mid-session toggle.
 */
export function getLatestUserFeatureFlags(): string[] | null {
  return getRegistry().lastFlagKeys;
}
