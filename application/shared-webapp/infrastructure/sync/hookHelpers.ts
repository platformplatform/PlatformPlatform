export function extractUrl(value: unknown): string | null {
  if (!value) {
    return null;
  }
  let parsed = value;
  if (typeof value === "string") {
    try {
      parsed = JSON.parse(value);
    } catch {
      return null;
    }
  }
  if (typeof parsed !== "object") {
    return null;
  }
  return ((parsed as Record<string, unknown>)?.Url as string | null) ?? null;
}

function toCamelCaseKeys(obj: unknown): unknown {
  if (Array.isArray(obj)) {
    return obj.map(toCamelCaseKeys);
  }
  if (obj !== null && typeof obj === "object") {
    return Object.fromEntries(
      Object.entries(obj as Record<string, unknown>).map(([key, val]) => [
        key.charAt(0).toLowerCase() + key.slice(1),
        toCamelCaseKeys(val)
      ])
    );
  }
  return obj;
}

export function castParsed<T>(value: unknown): T | null {
  if (!value) {
    return null;
  }
  let parsed = value;
  if (typeof value === "string") {
    try {
      parsed = JSON.parse(value);
    } catch {
      return null;
    }
  }
  if (typeof parsed !== "object") {
    return null;
  }
  return toCamelCaseKeys(parsed) as T;
}

export interface PaymentMethod {
  brand: string;
  last4: string;
  expMonth: number;
  expYear: number;
}

export interface BillingAddress {
  line1: string | null;
  line2: string | null;
  postalCode: string | null;
  city: string | null;
  state: string | null;
  country: string | null;
}

export interface BillingInfo {
  name: string | null;
  address: BillingAddress | null;
  email: string | null;
  taxId: string | null;
}
