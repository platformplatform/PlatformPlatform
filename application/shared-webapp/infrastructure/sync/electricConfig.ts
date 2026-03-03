import type { ShapeStreamOptions } from "@electric-sql/client";
import { snakeCamelMapper } from "@electric-sql/client";

const ACCOUNT_ELECTRIC_SHAPE_URL = "/api/account/electric/v1/shape";

export type ElectricTable = "users" | "tenants" | "subscriptions" | "sessions";

export function createShapeOptions(table: ElectricTable): ShapeStreamOptions {
  return {
    url: `${import.meta.env.PUBLIC_URL}${ACCOUNT_ELECTRIC_SHAPE_URL}`,
    params: {
      table
    },
    parser: {
      int8: (value: string) => value
    },
    columnMapper: snakeCamelMapper()
  };
}

export function getElectricOffset(response: Response): number | undefined {
  const offset = response.headers.get("electric-offset");
  return offset != null ? Number.parseInt(offset, 10) : undefined;
}
