import type { ReactNode } from "react";
import { api, TenantState } from "@/shared/lib/api/client";
import SuspendedPage from "./SuspendedPage";

interface TenantStateGuardProps {
  children: ReactNode;
  pathname: string;
}

export default function TenantStateGuard({ children, pathname }: Readonly<TenantStateGuardProps>) {
  const { data: tenant } = api.useQuery("get", "/api/account/tenants/current");

  const isSubscriptionPage = pathname.startsWith("/account/subscription");

  if (tenant?.state === TenantState.Suspended && !isSubscriptionPage) {
    return <SuspendedPage />;
  }

  return children;
}
