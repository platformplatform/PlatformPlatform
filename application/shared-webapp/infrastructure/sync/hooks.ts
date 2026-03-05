import { eq, isNull, not, useLiveQuery } from "@tanstack/react-db";
import { useMemo } from "react";
import { subscriptionCollection, tenantCollection, userCollection } from "./collections";

function extractUrl(value: unknown): string | null {
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

export function useUsers() {
  const { data: rawData, ...rest } = useLiveQuery((q) =>
    q
      .from({ users: userCollection })
      .where(({ users }) => isNull(users.deletedAt))
      .select(({ users }) => ({
        id: users.id,
        createdAt: users.createdAt,
        modifiedAt: users.modifiedAt,
        email: users.email,
        firstName: users.firstName,
        lastName: users.lastName,
        title: users.title,
        role: users.role,
        emailConfirmed: users.emailConfirmed,
        avatar: users.avatar,
        locale: users.locale,
        lastSeenAt: users.lastSeenAt
      }))
  );

  const data = useMemo(
    () => rawData.map(({ avatar, ...fields }) => ({ ...fields, avatarUrl: extractUrl(avatar) })),
    [rawData]
  );

  return { ...rest, data };
}

export function useDeletedUsers() {
  const { data: rawData, ...rest } = useLiveQuery((q) =>
    q
      .from({ users: userCollection })
      .where(({ users }) => not(isNull(users.deletedAt)))
      .select(({ users }) => ({
        id: users.id,
        createdAt: users.createdAt,
        modifiedAt: users.modifiedAt,
        email: users.email,
        firstName: users.firstName,
        lastName: users.lastName,
        title: users.title,
        role: users.role,
        emailConfirmed: users.emailConfirmed,
        avatar: users.avatar,
        deletedAt: users.deletedAt
      }))
  );

  const data = useMemo(
    () => rawData.map(({ avatar, ...fields }) => ({ ...fields, avatarUrl: extractUrl(avatar) })),
    [rawData]
  );

  return { ...rest, data };
}

export function useUser(userId: string) {
  const { data: rawData, ...rest } = useLiveQuery(
    (q) =>
      q
        .from({ users: userCollection })
        .where(({ users }) => eq(users.id, userId))
        .select(({ users }) => ({
          id: users.id,
          createdAt: users.createdAt,
          modifiedAt: users.modifiedAt,
          email: users.email,
          firstName: users.firstName,
          lastName: users.lastName,
          title: users.title,
          role: users.role,
          emailConfirmed: users.emailConfirmed,
          avatar: users.avatar,
          locale: users.locale,
          lastSeenAt: users.lastSeenAt
        }))
        .findOne(),
    [userId]
  );

  const data = useMemo(() => {
    if (!rawData) {
      return undefined;
    }
    const { avatar, ...fields } = rawData;
    return { ...fields, avatarUrl: extractUrl(avatar) };
  }, [rawData]);

  return { ...rest, data };
}

export function useTenant(tenantId: string) {
  const { data: rawData, ...rest } = useLiveQuery(
    (q) =>
      q
        .from({ tenants: tenantCollection })
        .where(({ tenants }) => eq(tenants.id, tenantId))
        .select(({ tenants }) => ({
          id: tenants.id,
          createdAt: tenants.createdAt,
          modifiedAt: tenants.modifiedAt,
          name: tenants.name,
          state: tenants.state,
          suspensionReason: tenants.suspensionReason,
          logo: tenants.logo,
          plan: tenants.plan,
          contactInfo: tenants.contactInfo
        }))
        .findOne(),
    [tenantId]
  );

  const data = useMemo(() => {
    if (!rawData) {
      return undefined;
    }
    const { logo, contactInfo, ...fields } = rawData;
    return {
      ...fields,
      logoUrl: extractUrl(logo),
      contactInfo: castParsed<ContactInfo>(contactInfo)
    };
  }, [rawData]);

  return { ...rest, data };
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

function castParsed<T>(value: unknown): T | null {
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

export interface PaymentTransaction {
  id: string;
  amount: number;
  currency: string;
  status: string;
  date: string;
  failureReason: string | null;
  invoiceUrl: string | null;
  creditNoteUrl: string | null;
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

export interface ContactInfo {
  address: string | null;
  postalCode: string | null;
  city: string | null;
  state: string | null;
  country: string | null;
  phoneNumber: string | null;
}

export function useSubscription() {
  const isOwner = import.meta.user_info_env.role === "Owner";

  const { data: rawData, ...rest } = useLiveQuery(
    (q) =>
      isOwner
        ? q
            .from({ subscriptions: subscriptionCollection })
            .select(({ subscriptions }) => ({
              id: subscriptions.id,
              createdAt: subscriptions.createdAt,
              modifiedAt: subscriptions.modifiedAt,
              plan: subscriptions.plan,
              scheduledPlan: subscriptions.scheduledPlan,
              cancelAtPeriodEnd: subscriptions.cancelAtPeriodEnd,
              firstPaymentFailedAt: subscriptions.firstPaymentFailedAt,
              cancellationReason: subscriptions.cancellationReason,
              cancellationFeedback: subscriptions.cancellationFeedback,
              currentPriceAmount: subscriptions.currentPriceAmount,
              currentPriceCurrency: subscriptions.currentPriceCurrency,
              currentPeriodEnd: subscriptions.currentPeriodEnd,
              paymentTransactions: subscriptions.paymentTransactions,
              paymentMethod: subscriptions.paymentMethod,
              billingInfo: subscriptions.billingInfo
            }))
            .findOne()
        : undefined,
    [isOwner]
  );

  const data = useMemo(() => {
    if (!rawData) {
      return undefined;
    }
    const { paymentMethod, billingInfo, paymentTransactions, firstPaymentFailedAt, currentPriceAmount, ...fields } =
      rawData;
    return {
      ...fields,
      currentPriceAmount: currentPriceAmount != null ? Number(currentPriceAmount) : null,
      hasStripeCustomer: currentPriceAmount != null,
      hasStripeSubscription: fields.currentPeriodEnd != null,
      isPaymentFailed: firstPaymentFailedAt != null,
      paymentTransactions: castParsed<PaymentTransaction[]>(paymentTransactions) ?? [],
      paymentMethod: castParsed<PaymentMethod>(paymentMethod),
      billingInfo: castParsed<BillingInfo>(billingInfo)
    };
  }, [rawData]);

  return { ...rest, data };
}
