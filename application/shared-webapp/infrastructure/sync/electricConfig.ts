import type { ShapeStreamOptions } from "@electric-sql/client";

import { FetchError, snakeCamelMapper } from "@electric-sql/client";

const ACCOUNT_ELECTRIC_SHAPE_URL = "/api/account/electric/v1/shape";

export type ElectricTable = "users" | "tenants" | "subscriptions";

const staleShapes = new Set<string>();
let isReloading = false;

function isStaleCacheError(error: Error): boolean {
  return error instanceof FetchError && error.status === 502 && error.message.includes("stale cached responses");
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
    onError: (error) => {
      if (error instanceof FetchError && error.status === 403) {
        return;
      }
      if (staleShapes.has(table)) {
        return;
      }
      if (isStaleCacheError(error)) {
        staleShapes.add(table);
        console.error(`[Electric] Shape "${table}" has a permanently stale handle. Reloading to resync.`);
        if (!isReloading) {
          isReloading = true;
          window.location.reload();
        }
        return;
      }
      return {};
    }
  };
}
