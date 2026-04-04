import type { ShapeStreamOptions } from "@electric-sql/client";

import { FetchError, snakeCamelMapper } from "@electric-sql/client";
import { toast } from "sonner";

const ACCOUNT_ELECTRIC_SHAPE_URL = "/api/account/electric/v1/shape";

export type ElectricTable = "users" | "tenants" | "subscriptions";

const MAX_CONSECUTIVE_FAILURES = 3;

const staleShapes = new Set<string>();
const failureCounts = new Map<string, number>();
let isReloading = false;
let hasShownSyncError = false;

function isStaleCacheError(error: Error): boolean {
  return error instanceof FetchError && error.status === 502 && error.message.includes("stale cached responses");
}

function isExpiredHandleError(error: Error): boolean {
  return error instanceof FetchError && error.status === 500;
}

function triggerReload(table: string, reason: string): void {
  staleShapes.add(table);
  console.error(`[Electric] Shape "${table}" ${reason}. Reloading to resync.`);
  if (!isReloading) {
    isReloading = true;
    window.location.reload();
  }
}

function tripCircuitBreaker(table: string): void {
  console.error(`[Electric] Shape "${table}" circuit breaker tripped after ${MAX_CONSECUTIVE_FAILURES} failures.`);
  if (!hasShownSyncError) {
    hasShownSyncError = true;
    toast.error("Live sync interrupted", {
      description: "Reload the page to reconnect.",
      duration: Infinity,
      action: { label: "Reload", onClick: () => window.location.reload() }
    });
  }
}

export function createShapeOptions(table: ElectricTable): ShapeStreamOptions {
  return {
    url: `${import.meta.env.PUBLIC_URL}${ACCOUNT_ELECTRIC_SHAPE_URL}`,
    params: {
      table
    },
    parser: {
      int8: (value: string) => value
    },
    columnMapper: snakeCamelMapper(),
    liveSse: true,
    backoffOptions: {
      initialDelay: 1000,
      maxDelay: 30000,
      multiplier: 2
    },
    onError: (error) => {
      if (error instanceof FetchError && error.status === 403) {
        return;
      }
      if (staleShapes.has(table)) {
        return;
      }
      if (isStaleCacheError(error)) {
        triggerReload(table, "has a permanently stale handle");
        return;
      }
      if (isExpiredHandleError(error)) {
        triggerReload(table, "has an expired or invalid handle");
        return;
      }

      const count = (failureCounts.get(table) ?? 0) + 1;
      failureCounts.set(table, count);

      if (count >= MAX_CONSECUTIVE_FAILURES) {
        tripCircuitBreaker(table);
        return;
      }

      return {};
    }
  };
}
