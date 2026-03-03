import type { Row } from "@electric-sql/client";

export type UserRow = Row & {
  id: string;
  tenantId: string;
  createdAt: string;
  modifiedAt: string | null;
  email: string;
  firstName: string | null;
  lastName: string | null;
  title: string | null;
  role: string;
  emailConfirmed: boolean;
  avatar: string;
  locale: string;
  lastSeenAt: string | null;
  externalIdentities: string;
  deletedAt: string | null;
};

export type TenantRow = Row & {
  id: string;
  createdAt: string;
  modifiedAt: string | null;
  name: string;
  state: string;
  suspensionReason: string | null;
  suspendedAt: string | null;
  logo: string;
  deletedAt: string | null;
};

export type SubscriptionRow = Row & {
  id: string;
  tenantId: string;
  createdAt: string;
  modifiedAt: string | null;
  plan: string;
  scheduledPlan: string | null;
  stripeCustomerId: string | null;
  stripeSubscriptionId: string | null;
  currentPriceAmount: string | null;
  currentPriceCurrency: string | null;
  currentPeriodEnd: string | null;
  cancelAtPeriodEnd: boolean;
  firstPaymentFailedAt: string | null;
  cancellationReason: string | null;
  cancellationFeedback: string | null;
  paymentTransactions: string;
  paymentMethod: string | null;
  billingInfo: string | null;
};

export type SessionRow = Row & {
  id: string;
  tenantId: string;
  createdAt: string;
  modifiedAt: string | null;
  userId: string;
  refreshTokenJti: string;
  previousRefreshTokenJti: string | null;
  refreshTokenVersion: number;
  loginMethod: string;
  deviceType: string;
  userAgent: string;
  ipAddress: string;
  revokedAt: string | null;
  revokedReason: string | null;
};
