/**
 * Global navigation guard registry for federated micro-frontends.
 * When navigating from a federated module (e.g., AccountApp) back to the shell router,
 * TanStack Router's useBlocker only works within its own router instance.
 * This registry allows components to register guards that can be checked globally
 * before cross-router navigation occurs.
 */
type NavigationGuardFn = () => boolean;
const guards = new Set<NavigationGuardFn>();

export function registerNavigationGuard(guard: NavigationGuardFn): () => void {
  guards.add(guard);
  return () => {
    guards.delete(guard);
  };
}

export function shouldBlockNavigation(): boolean {
  for (const guard of guards) {
    if (guard()) {
      return true;
    }
  }
  return false;
}
