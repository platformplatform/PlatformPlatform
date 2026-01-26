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
