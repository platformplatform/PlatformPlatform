import type { Page } from "@playwright/test";

declare global {
  interface Window {
    __electricCollections?: {
      userCollection: unknown;
      tenantCollection: unknown;
      subscriptionCollection: unknown;
    };
  }
}

const ELECTRIC_SHAPE_URL_PATTERN = "**/api/account/electric/v1/shape*";

/**
 * Converts a camelCase key to snake_case.
 * Electric SQL delivers column names in snake_case, and the client's snakeCamelMapper converts them.
 */
function toSnakeCase(key: string): string {
  return key.replace(/[A-Z]/g, (letter) => `_${letter.toLowerCase()}`);
}

/**
 * Converts an object's keys from camelCase to snake_case.
 */
function toSnakeCaseKeys(obj: Record<string, unknown>): Record<string, unknown> {
  const result: Record<string, unknown> = {};
  for (const [key, value] of Object.entries(obj)) {
    result[toSnakeCase(key)] = value;
  }
  return result;
}

/**
 * Builds the electric-schema header value from row data.
 * Maps each column to a simple type descriptor.
 */
function buildSchemaHeader(row: Record<string, unknown>): string {
  const schema: Record<string, { type: string }> = {};
  for (const key of Object.keys(row)) {
    const snakeKey = toSnakeCase(key);
    schema[snakeKey] = { type: "text" };
  }
  return JSON.stringify(schema);
}

/**
 * Builds a valid Electric shape snapshot response with mock data.
 *
 * The Electric client uses long-polling for the initial snapshot request.
 * The response body must be a JSON array of Message objects (not an object wrapper).
 * Each row is a ChangeMessage with headers.operation, key, and value.
 * The array ends with a ControlMessage { headers: { control: "up-to-date" } }.
 */
function buildShapeResponse(rows: Record<string, unknown>[]) {
  const snakeCaseRows = rows.map(toSnakeCaseKeys);
  const messages: Record<string, unknown>[] = snakeCaseRows.map((row, index) => ({
    headers: { operation: "insert" },
    key: String(row.id ?? index),
    value: row
  }));
  messages.push({ headers: { control: "up-to-date" } });

  const schemaHeader = snakeCaseRows.length > 0 ? buildSchemaHeader(rows[0]) : "{}";

  return {
    body: JSON.stringify(messages),
    headers: {
      "content-type": "application/json",
      "electric-handle": `mock-handle-${Date.now()}`,
      "electric-offset": "0_0",
      "electric-schema": schemaHeader
    }
  };
}

type ElectricTable = "users" | "tenants" | "subscriptions";

interface ActiveMock {
  table: ElectricTable;
  getRows: () => Record<string, unknown>[];
}

const activeMocks = new WeakMap<Page, ActiveMock[]>();

function getActiveMocks(page: Page): ActiveMock[] {
  if (!activeMocks.has(page)) {
    activeMocks.set(page, []);
  }
  return activeMocks.get(page)!;
}

/**
 * Mock an Electric shape stream for a specific table.
 *
 * Intercepts all Electric shape HTTP requests for the given table and returns
 * mock data in the Electric protocol format. For initial (non-live) requests,
 * returns a snapshot response with the mock rows. For live/SSE requests,
 * returns 403 to silently stop the sync (the Electric client's onError handler
 * in electricConfig.ts returns undefined for 403, which stops retries).
 *
 * @param page - Playwright page
 * @param table - Electric table name ("subscriptions", "tenants", "users")
 * @param getRows - Function that returns the current mock row data (called on each request)
 */
export async function mockElectricShape(
  page: Page,
  table: ElectricTable,
  getRows: () => Record<string, unknown>[]
): Promise<void> {
  const mocks = getActiveMocks(page);
  mocks.push({ table, getRows });

  // Only set up the route handler once per page
  if (mocks.length === 1) {
    await page.route(ELECTRIC_SHAPE_URL_PATTERN, async (route) => {
      const url = new URL(route.request().url());
      const requestedTable = url.searchParams.get("table");
      const isLive = url.searchParams.get("live") === "true";

      const mock = getActiveMocks(page).find((m) => m.table === requestedTable);
      if (!mock) {
        // No mock for this table, pass through to real server
        await route.fallback();
        return;
      }

      if (isLive) {
        // For live/SSE requests, return 403 to silently stop sync
        // The onError handler in electricConfig.ts returns undefined for 403
        await route.fulfill({ status: 403 });
        return;
      }

      // For initial snapshot requests, return mock data
      const response = buildShapeResponse(mock.getRows());
      await route.fulfill({
        status: 200,
        body: response.body,
        headers: response.headers
      });
    });
  }
}

/**
 * Remove the Electric shape mock for a specific table.
 * If no more mocks remain, removes the route handler entirely.
 */
export async function unmockElectricShape(page: Page, table: ElectricTable): Promise<void> {
  const mocks = getActiveMocks(page);
  const index = mocks.findIndex((m) => m.table === table);
  if (index !== -1) {
    mocks.splice(index, 1);
  }
  if (mocks.length === 0) {
    await page.unroute(ELECTRIC_SHAPE_URL_PATTERN);
  }
}

/**
 * Helper to build a SubscriptionRow for Electric collection mocking.
 *
 * Accepts test-friendly parameters matching the old REST mock format and converts
 * them to the Electric collection row format (camelCase keys, JSON stringified
 * value objects with PascalCase keys).
 */
export function buildSubscriptionRow(params: {
  id?: string;
  plan: string;
  scheduledPlan?: string | null;
  currentPriceAmount?: number | null;
  currentPriceCurrency?: string | null;
  currentPeriodEnd?: string | null;
  cancelAtPeriodEnd?: boolean;
  isPaymentFailed?: boolean;
  paymentMethod?: { brand: string; last4: string; expMonth: number; expYear: number } | null;
  billingInfo?: {
    name: string | null;
    address: {
      line1: string | null;
      line2: string | null;
      postalCode: string | null;
      city: string | null;
      state: string | null;
      country: string;
    } | null;
    email: string | null;
    taxId: string | null;
  } | null;
  paymentTransactions?: Array<{
    id: string;
    amount: number;
    currency: string;
    status: string;
    date: string;
    invoiceUrl: string | null;
    creditNoteUrl: string | null;
  }>;
}): Record<string, unknown> {
  return {
    id: params.id ?? "sub_mock",
    createdAt: "2026-01-01T00:00:00Z",
    modifiedAt: null,
    plan: params.plan,
    scheduledPlan: params.scheduledPlan ?? null,
    cancelAtPeriodEnd: params.cancelAtPeriodEnd ?? false,
    firstPaymentFailedAt: params.isPaymentFailed ? "2026-03-01T00:00:00Z" : null,
    cancellationReason: null,
    cancellationFeedback: null,
    currentPriceAmount: params.currentPriceAmount != null ? String(params.currentPriceAmount) : null,
    currentPriceCurrency: params.currentPriceCurrency ?? null,
    currentPeriodEnd: params.currentPeriodEnd ?? null,
    paymentTransactions: JSON.stringify(
      (params.paymentTransactions ?? []).map((t) => ({
        Id: t.id,
        Amount: t.amount,
        Currency: t.currency,
        Status: t.status,
        Date: t.date,
        FailureReason: null,
        InvoiceUrl: t.invoiceUrl,
        CreditNoteUrl: t.creditNoteUrl
      }))
    ),
    paymentMethod: params.paymentMethod
      ? JSON.stringify({
          Brand: params.paymentMethod.brand,
          Last4: params.paymentMethod.last4,
          ExpMonth: params.paymentMethod.expMonth,
          ExpYear: params.paymentMethod.expYear
        })
      : null,
    billingInfo: params.billingInfo
      ? JSON.stringify({
          Name: params.billingInfo.name,
          Address: params.billingInfo.address
            ? {
                Line1: params.billingInfo.address.line1,
                Line2: params.billingInfo.address.line2,
                PostalCode: params.billingInfo.address.postalCode,
                City: params.billingInfo.address.city,
                State: params.billingInfo.address.state,
                Country: params.billingInfo.address.country
              }
            : null,
          Email: params.billingInfo.email,
          TaxId: params.billingInfo.taxId
        })
      : null
  };
}

/**
 * Update subscription data in the Electric collection via page.evaluate().
 * Injects data directly into the collection's internal sync transaction pipeline,
 * which triggers useLiveQuery reactivity.
 *
 * Used in test steps where a mutation triggers useSubscriptionPolling, which
 * watches the collection for changes to show success toasts.
 *
 * Uses camelCase keys because the Electric snakeCamelMapper has already
 * converted column names by the time data reaches the collection store.
 */
export async function updateSubscriptionCollection(
  page: Page,
  params: Parameters<typeof buildSubscriptionRow>[0]
): Promise<void> {
  const row = buildSubscriptionRow(params);
  await page.evaluate((data) => {
    const collection = window.__electricCollections?.subscriptionCollection;
    if (!collection) return;
    const state = (collection as any)._state;
    state.pendingSyncedTransactions.push({
      committed: true,
      operations: [{ type: "insert", key: data.id, value: data }],
      deletedKeys: new Set(),
      immediate: true
    });
    state.commitPendingTransactions();
  }, row);
}

/**
 * Update tenant data in the Electric collection via page.evaluate().
 */
export async function updateTenantCollection(
  page: Page,
  params: Parameters<typeof buildTenantRow>[0]
): Promise<void> {
  const row = buildTenantRow(params);
  await page.evaluate((data) => {
    const collection = window.__electricCollections?.tenantCollection;
    if (!collection) return;
    const state = (collection as any)._state;
    state.pendingSyncedTransactions.push({
      committed: true,
      operations: [{ type: "update", key: data.id, value: data }],
      deletedKeys: new Set(),
      immediate: true
    });
    state.commitPendingTransactions();
  }, row);
}

/**
 * Helper to build a TenantRow for Electric collection mocking.
 */
export function buildTenantRow(params: {
  id?: string;
  name?: string;
  state: string;
  plan?: string;
}): Record<string, unknown> {
  return {
    id: params.id ?? "tenant_mock",
    createdAt: "2026-01-01T00:00:00Z",
    modifiedAt: null,
    name: params.name ?? "Test Organization",
    state: params.state,
    suspensionReason: null,
    logo: "",
    plan: params.plan ?? "Basis"
  };
}
