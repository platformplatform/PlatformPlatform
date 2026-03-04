import type { Row } from "@electric-sql/client";

export type UserRow = Row & {
  id: string;
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
  deletedAt: string | null;
};

export type TenantRow = Row & {
  id: string;
  createdAt: string;
  modifiedAt: string | null;
  name: string;
  state: string;
  suspensionReason: string | null;
  logo: string;
  plan: string;
};

export type SubscriptionRow = Row & {
  id: string;
  createdAt: string;
  modifiedAt: string | null;
  plan: string;
  scheduledPlan: string | null;
  cancelAtPeriodEnd: boolean;
  firstPaymentFailedAt: string | null;
  cancellationReason: string | null;
  cancellationFeedback: string | null;
  currentPriceAmount: string | null;
  currentPriceCurrency: string | null;
  currentPeriodEnd: string | null;
  paymentTransactions: string;
  paymentMethod: string | null;
  billingInfo: string | null;
};
