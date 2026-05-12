import type { ReactNode } from "react";

import { Trans } from "@lingui/react/macro";
import { Card } from "@repo/ui/components/Card";
import { Empty, EmptyDescription, EmptyHeader, EmptyTitle } from "@repo/ui/components/Empty";
import { Skeleton } from "@repo/ui/components/Skeleton";

import type { components } from "@/shared/lib/api/client";

import { CurrentPlanDetails } from "./CurrentPlanDetails";

type TenantDetailResponse = components["schemas"]["TenantDetailResponse"];

interface AccountCurrentPlanCardProps {
  tenant: TenantDetailResponse | undefined;
  isLoading: boolean;
}

export function AccountCurrentPlanCard({ tenant, isLoading }: Readonly<AccountCurrentPlanCardProps>) {
  if (isLoading || !tenant) {
    return <CurrentPlanShell>{renderSkeleton()}</CurrentPlanShell>;
  }

  const isFree = tenant.subscribedSince === null && !tenant.hasEverSubscribed;
  if (isFree) {
    return (
      <CurrentPlanShell>
        <CurrentPlanEmpty title={<Trans>No plan</Trans>} description={<Trans>No paid plan yet.</Trans>} />
      </CurrentPlanShell>
    );
  }

  return (
    <CurrentPlanShell>
      <CurrentPlanDetails tenant={tenant} />
    </CurrentPlanShell>
  );
}

function CurrentPlanShell({ children }: Readonly<{ children: ReactNode }>) {
  return (
    <section className="flex h-full flex-col">
      <h4 className="mb-3 whitespace-nowrap">
        <Trans>Current plan</Trans>
      </h4>
      {children}
    </section>
  );
}

function CurrentPlanEmpty({ title, description }: Readonly<{ title: ReactNode; description: ReactNode }>) {
  return (
    <Empty className="h-[8.375rem] flex-none border bg-card p-4 md:p-4 lg:h-auto lg:min-h-[20.75rem] lg:flex-1 lg:p-12">
      <EmptyHeader>
        <EmptyTitle>{title}</EmptyTitle>
        <EmptyDescription>{description}</EmptyDescription>
      </EmptyHeader>
    </Empty>
  );
}

function renderSkeleton() {
  return (
    <Card className="flex-1 gap-4 rounded-lg p-5 py-5 shadow-none">
      <Skeleton className="h-5 w-24" />
      <Skeleton className="h-9 w-40" />
      <Skeleton className="h-4 w-32" />
      <Skeleton className="h-px w-full" />
      <Skeleton className="h-4 w-full" />
      <Skeleton className="h-4 w-full" />
    </Card>
  );
}
