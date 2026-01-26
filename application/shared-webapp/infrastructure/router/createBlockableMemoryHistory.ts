import { createHistory, type NavigationBlocker, parseHref, type RouterHistory } from "@tanstack/history";

/**
 * Creates a memory history with blocker support.
 * TanStack Router's createMemoryHistory does not pass getBlockers/setBlockers
 * to createHistory, which makes useBlocker and history.block() no-ops.
 * This version adds blocker support so navigation guards work correctly
 * in federated micro-frontend scenarios using memory routers.
 */
export function createBlockableMemoryHistory(opts: {
  initialEntries: Array<string>;
  initialIndex?: number;
}): RouterHistory {
  const entries = [...opts.initialEntries];
  let index = opts.initialIndex ? Math.min(Math.max(opts.initialIndex, 0), entries.length - 1) : entries.length - 1;

  const tanstackRouterKeyProp = "__TSR_key";
  const tanstackRouterIndexProp = "__TSR_index";
  const states = entries.map((_, i) => {
    const key = crypto.randomUUID();
    return { key, [tanstackRouterKeyProp]: key, [tanstackRouterIndexProp]: i };
  });

  let blockers: Array<NavigationBlocker> = [];

  return createHistory({
    getLocation: () => parseHref(entries[index] ?? "/", states[index]),
    getLength: () => entries.length,
    pushState: (path, state) => {
      if (index < entries.length - 1) {
        entries.splice(index + 1);
        states.splice(index + 1);
      }
      states.push(state);
      entries.push(path);
      index = Math.max(entries.length - 1, 0);
    },
    replaceState: (path, state) => {
      states[index] = state;
      entries[index] = path;
    },
    back: () => {
      index = Math.max(index - 1, 0);
    },
    forward: () => {
      index = Math.min(index + 1, entries.length - 1);
    },
    go: (n) => {
      index = Math.min(Math.max(index + n, 0), entries.length - 1);
    },
    createHref: (path) => path,
    getBlockers: () => blockers,
    setBlockers: (newBlockers) => {
      blockers = newBlockers;
    }
  });
}
