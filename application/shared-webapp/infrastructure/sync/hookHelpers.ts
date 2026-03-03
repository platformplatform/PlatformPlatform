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
  return parsed as T;
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
  country: string;
}

export interface BillingInfo {
  name: string | null;
  address: BillingAddress | null;
  email: string | null;
  taxId: string | null;
}
