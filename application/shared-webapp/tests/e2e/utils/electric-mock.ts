import type { Page, Route } from "@playwright/test";

async function getPageTenantId(page: Page): Promise<string> {
  return page.evaluate(() => {
    const meta = document.head.querySelector('meta[name="userInfoEnv"]');
    if (!meta) return "unknown";
    try {
      const info = JSON.parse(meta.getAttribute("content") ?? "{}");
      return info.tenantId ?? "unknown";
    } catch {
      return "unknown";
    }
  });
}

type ElectricTable = "subscriptions" | "tenants" | "users";

interface ElectricColumnSchema {
  type: string;
  dims?: number;
  not_null?: boolean;
}

const subscriptionSchema: Record<string, ElectricColumnSchema> = {
  id: { type: "text", not_null: true },
  created_at: { type: "timestamptz", not_null: true },
  modified_at: { type: "timestamptz" },
  plan: { type: "text", not_null: true },
  scheduled_plan: { type: "text" },
  cancel_at_period_end: { type: "bool", not_null: true },
  first_payment_failed_at: { type: "timestamptz" },
  cancellation_reason: { type: "text" },
  cancellation_feedback: { type: "text" },
  current_price_amount: { type: "int8" },
  current_price_currency: { type: "text" },
  current_period_end: { type: "timestamptz" },
  payment_transactions: { type: "jsonb", not_null: true },
  payment_method: { type: "jsonb" },
  billing_info: { type: "jsonb" }
};

const tenantSchema: Record<string, ElectricColumnSchema> = {
  id: { type: "text", not_null: true },
  created_at: { type: "timestamptz", not_null: true },
  modified_at: { type: "timestamptz" },
  name: { type: "text", not_null: true },
  state: { type: "text", not_null: true },
  suspension_reason: { type: "text" },
  logo: { type: "jsonb", not_null: true },
  plan: { type: "text", not_null: true }
};

const schemas: Record<ElectricTable, Record<string, ElectricColumnSchema>> = {
  subscriptions: subscriptionSchema,
  tenants: tenantSchema,
  users: { id: { type: "text", not_null: true }, created_at: { type: "timestamptz", not_null: true }, modified_at: { type: "timestamptz" } }
};

const offsetCounters = new Map<string, number>();

function buildElectricResponse(
  table: ElectricTable,
  rows: Record<string, string | null>[],
  handle: string
) {
  const schema = schemas[table];
  const counter = (offsetCounters.get(table) ?? 0) + 1;
  offsetCounters.set(table, counter);

  const messages = [
    ...rows.map((row) => ({
      key: row.id ?? "unknown",
      value: row,
      headers: { operation: "insert" },
      offset: `${counter}_0`
    })),
    { headers: { control: "up-to-date" } }
  ];

  return {
    headers: {
      "content-type": "application/json",
      "electric-schema": JSON.stringify(schema),
      "electric-handle": handle,
      "electric-offset": `${counter}_0`,
      "electric-cursor": `${counter}_0`,
      "electric-up-to-date": ""
    },
    body: messages
  };
}

interface PaymentTransactionMockData {
  id: string;
  amount: number;
  currency: string;
  status: string;
  date: string;
  failureReason?: string | null;
  invoiceUrl?: string | null;
  creditNoteUrl?: string | null;
}

interface SubscriptionMockData {
  id?: string;
  plan: string;
  scheduledPlan?: string | null;
  currentPriceAmount?: number | null;
  currentPriceCurrency?: string | null;
  currentPeriodEnd?: string | null;
  cancelAtPeriodEnd?: boolean;
  isPaymentFailed?: boolean;
  firstPaymentFailedAt?: string | null;
  paymentTransactions?: PaymentTransactionMockData[];
  paymentMethod?: { brand: string; last4: string; expMonth: number; expYear: number } | null;
  billingInfo?: {
    name: string | null;
    address?: {
      line1: string | null;
      line2: string | null;
      postalCode: string | null;
      city: string | null;
      state: string | null;
      country: string;
    } | null;
    email: string | null;
    taxId?: string | null;
  } | null;
}

function toSubscriptionRow(data: SubscriptionMockData): Record<string, string | null> {
  return {
    id: data.id ?? "sub_mock",
    created_at: "2026-01-01T00:00:00Z",
    modified_at: null,
    plan: data.plan,
    scheduled_plan: data.scheduledPlan ?? null,
    cancel_at_period_end: String(data.cancelAtPeriodEnd ?? false),
    first_payment_failed_at:
      data.firstPaymentFailedAt ?? (data.isPaymentFailed ? "2026-03-01T00:00:00Z" : null),
    cancellation_reason: null,
    cancellation_feedback: null,
    current_price_amount: data.currentPriceAmount != null ? String(data.currentPriceAmount) : null,
    current_price_currency: data.currentPriceCurrency ?? null,
    current_period_end: data.currentPeriodEnd ?? null,
    payment_transactions: JSON.stringify(data.paymentTransactions ?? []),
    payment_method: data.paymentMethod ? JSON.stringify(data.paymentMethod) : null,
    billing_info: data.billingInfo ? JSON.stringify(data.billingInfo) : null
  };
}

interface TenantMockData {
  id?: string;
  name?: string;
  state: string;
  suspensionReason?: string | null;
  logoUrl?: string | null;
  plan?: string;
}

function toTenantRow(data: TenantMockData, resolvedTenantId?: string): Record<string, string | null> {
  return {
    id: data.id ?? resolvedTenantId ?? "mock-tenant",
    created_at: "2026-01-01T00:00:00Z",
    modified_at: null,
    name: data.name ?? "Test Organization",
    state: data.state,
    suspension_reason: data.suspensionReason ?? null,
    logo: data.logoUrl ? JSON.stringify({ Url: data.logoUrl }) : "{}",
    plan: data.plan ?? "Basis"
  };
}

function isElectricShapeUrl(url: URL): boolean {
  return url.pathname.includes("/electric/v1/shape");
}

const activeHandlers = new Map<string, (route: Route) => Promise<void>>();

function handlerKey(page: Page, table: ElectricTable): string {
  return `${(page as unknown as { _guid?: string })._guid ?? "page"}-${table}`;
}

/**
 * Mock the Electric shape endpoint for a specific table.
 * Intercepts all Electric shape requests and filters by table parameter.
 * The dataFn is called on each request, allowing dynamic data changes.
 */
export async function mockElectricShape(
  page: Page,
  table: ElectricTable,
  dataFn: () => Record<string, string | null>[]
): Promise<void> {
  const key = handlerKey(page, table);

  const existing = activeHandlers.get(key);
  if (existing) {
    await page.unroute(isElectricShapeUrl, existing);
    activeHandlers.delete(key);
  }

  let lastSerializedData = "";
  const mockHandle = `mock-${table}-${Date.now()}`;
  let refetchSent = false;

  const handler = async (route: Route) => {
    const url = new URL(route.request().url());
    const requestTable = url.searchParams.get("table");
    if (requestTable !== table) {
      await route.fallback();
      return;
    }

    const requestOffset = url.searchParams.get("offset") ?? "-1";
    const requestHandle = url.searchParams.get("handle");

    if (!refetchSent && requestHandle && requestHandle !== mockHandle) {
      refetchSent = true;
      try {
        await route.fulfill({
          status: 409,
          headers: {
            "content-type": "application/json",
            "electric-handle": mockHandle
          },
          body: JSON.stringify([{ headers: { control: "must-refetch" } }])
        });
      } catch {
        // Route may already be handled if page navigated
      }
      return;
    }

    const rows = dataFn();
    const serialized = JSON.stringify(rows);
    const dataChanged = serialized !== lastSerializedData;

    if (requestOffset === "-1" || dataChanged) {
      lastSerializedData = serialized;
      const response = buildElectricResponse(table, rows, mockHandle);
      try {
        await route.fulfill({
          status: 200,
          headers: response.headers,
          body: JSON.stringify(response.body)
        });
      } catch {
        // Route may already be handled if page navigated
      }
    } else {
      const currentOffset = url.searchParams.get("offset") ?? "0_0";
      try {
        await route.fulfill({
          status: 200,
          headers: {
            "content-type": "application/json",
            "electric-schema": JSON.stringify(schemas[table]),
            "electric-handle": mockHandle,
            "electric-offset": currentOffset,
            "electric-cursor": currentOffset,
            "electric-up-to-date": ""
          },
          body: JSON.stringify([{ headers: { control: "up-to-date" } }])
        });
      } catch {
        // Route may already be handled if page navigated
      }
    }
  };

  activeHandlers.set(key, handler);
  await page.route(isElectricShapeUrl, handler);
}

/**
 * Mock the Electric subscription shape with a subscription-specific data format.
 * The dataFn is called on each request, allowing dynamic state changes.
 */
export async function mockElectricSubscription(
  page: Page,
  dataFn: () => SubscriptionMockData
): Promise<void> {
  await mockElectricShape(page, "subscriptions", () => [toSubscriptionRow(dataFn())]);
}

/**
 * Mock the Electric tenant shape with a tenant-specific data format.
 * The dataFn is called on each request, allowing dynamic state changes.
 */
export async function mockElectricTenant(
  page: Page,
  dataFn: () => TenantMockData
): Promise<void> {
  const tenantId = await getPageTenantId(page);
  await mockElectricShape(page, "tenants", () => [toTenantRow(dataFn(), tenantId)]);
}

/**
 * Remove Electric shape mocks for a specific table.
 */
export async function unmockElectricShape(page: Page, table: ElectricTable): Promise<void> {
  const key = handlerKey(page, table);
  const handler = activeHandlers.get(key);
  if (handler) {
    await page.unroute(isElectricShapeUrl, handler);
    activeHandlers.delete(key);
  }
}
