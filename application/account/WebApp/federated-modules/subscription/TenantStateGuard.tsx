import type { ReactNode } from "react";

import { useTenant } from "@repo/infrastructure/sync/hooks";

import { TenantState } from "@/shared/lib/api/client";

import SuspendedPage from "./SuspendedPage";

interface TenantStateGuardProps {
  children: ReactNode;
  pathname: string;
}

export default function TenantStateGuard({ children, pathname }: Readonly<TenantStateGuardProps>) {
  const { tenantId } = import.meta.user_info_env;
  const { data: tenant } = useTenant(tenantId ?? "");

  const isBillingPage = pathname.startsWith("/account/billing");

  if (tenant?.state === TenantState.Suspended && !isBillingPage) {
    return <SuspendedPage />;
  }

  return children;
}
