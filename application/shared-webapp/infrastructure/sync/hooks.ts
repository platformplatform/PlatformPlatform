import { eq, useLiveQuery } from "@tanstack/react-db";
import { useMemo } from "react";

import { sessionCollection, subscriptionCollection, tenantCollection, userCollection } from "./collections";

function parseJsonUrl(json: string | null): string | null {
  if (!json) {
    return null;
  }
  try {
    const parsed = JSON.parse(json);
    return parsed?.Url ?? null;
  } catch {
    return null;
  }
}

export function useUsers() {
  return useLiveQuery((q) =>
    q
      .from({ users: userCollection })
      .where(({ users }) => eq(users.deletedAt, null))
      .select(({ users }) => ({
        id: users.id,
        tenantId: users.tenantId,
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
}

export function useUser(userId: string) {
  const { data: rawData, ...rest } = useLiveQuery(
    (q) =>
      q
        .from({ users: userCollection })
        .where(({ users }) => eq(users.id, userId))
        .select(({ users }) => ({
          id: users.id,
          tenantId: users.tenantId,
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
    return { ...fields, avatarUrl: parseJsonUrl(avatar) };
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
          logo: tenants.logo
        }))
        .findOne(),
    [tenantId]
  );

  const data = useMemo(() => {
    if (!rawData) {
      return undefined;
    }
    const { logo, ...fields } = rawData;
    return { ...fields, logoUrl: parseJsonUrl(logo) };
  }, [rawData]);

  return { ...rest, data };
}

export function useSubscription(tenantId: string) {
  return useLiveQuery(
    (q) =>
      q
        .from({ subscriptions: subscriptionCollection })
        .where(({ subscriptions }) => eq(subscriptions.tenantId, tenantId))
        .select(({ subscriptions }) => ({
          id: subscriptions.id,
          tenantId: subscriptions.tenantId,
          createdAt: subscriptions.createdAt,
          modifiedAt: subscriptions.modifiedAt,
          plan: subscriptions.plan,
          scheduledPlan: subscriptions.scheduledPlan,
          currentPriceAmount: subscriptions.currentPriceAmount,
          currentPriceCurrency: subscriptions.currentPriceCurrency,
          currentPeriodEnd: subscriptions.currentPeriodEnd,
          cancelAtPeriodEnd: subscriptions.cancelAtPeriodEnd,
          paymentMethod: subscriptions.paymentMethod,
          billingInfo: subscriptions.billingInfo
        }))
        .findOne(),
    [tenantId]
  );
}

export function useSessions() {
  return useLiveQuery((q) =>
    q
      .from({ sessions: sessionCollection })
      .where(({ sessions }) => eq(sessions.revokedAt, null))
      .select(({ sessions }) => ({
        id: sessions.id,
        tenantId: sessions.tenantId,
        createdAt: sessions.createdAt,
        userId: sessions.userId,
        loginMethod: sessions.loginMethod,
        deviceType: sessions.deviceType,
        userAgent: sessions.userAgent,
        ipAddress: sessions.ipAddress
      }))
  );
}
