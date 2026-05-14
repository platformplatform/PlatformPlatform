import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Badge } from "@repo/ui/components/Badge";
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from "@repo/ui/components/Collapsible";
import { Empty, EmptyDescription, EmptyHeader, EmptyMedia, EmptyTitle } from "@repo/ui/components/Empty";
import { Skeleton } from "@repo/ui/components/Skeleton";
import { TextField } from "@repo/ui/components/TextField";
import { Building2Icon, ChevronDown } from "lucide-react";
import { useMemo, useState } from "react";

import { api, SubscriptionPlan } from "@/shared/lib/api/client";
import { getSubscriptionPlanLabel } from "@/shared/lib/api/labels";

import type { FeatureFlagInfo, FeatureFlagTenantInfo } from "./types";

import { PlanFeatureFlagTenantTable } from "./PlanFeatureFlagTenantTable";

// Plan-managed flags display every tenant grouped by plan; the section is not paginated because the plan
// inheritance view needs the full picture at a glance. Cap is high enough for current tenant counts.
const PLAN_TENANT_LIST_CAP = 1000;

export function PlanFeatureFlagInfoSection({ featureFlag }: Readonly<{ featureFlag: FeatureFlagInfo }>) {
  return (
    <div className="flex flex-col gap-4">
      <div className="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
        <div className="flex flex-col gap-0.5 text-sm text-muted-foreground">
          <span>
            <Trans>Name:</Trans> <span className="font-mono">{featureFlag.key}</span>
          </span>
          <span>
            <Trans>Required plan:</Trans> <Badge variant="outline">{featureFlag.requiredPlan}</Badge>
          </span>
        </div>
        <Badge variant={featureFlag.isActive ? "default" : "outline"}>
          {featureFlag.isActive ? t`Active` : t`Inactive`}
        </Badge>
      </div>
      <p className="text-sm text-muted-foreground">
        <Trans>
          This flag is managed by the subscription plan. It is automatically enabled for accounts on the required plan
          or higher.
        </Trans>
      </p>
    </div>
  );
}

export function PlanFeatureFlagTenantsSection({ flagKey }: Readonly<{ flagKey: string }>) {
  const [search, setSearch] = useState("");

  const { data, isLoading } = api.useQuery("get", "/api/back-office/feature-flags/{flagKey}/tenants", {
    params: {
      path: { flagKey },
      query: {
        PageSize: PLAN_TENANT_LIST_CAP
      }
    }
  });

  const filtered = useMemo(() => {
    const tenants = data?.tenants ?? [];
    const lowerSearch = search.toLowerCase();
    return search
      ? tenants.filter((tenant) => tenant.name.toLowerCase().includes(lowerSearch) || tenant.id.includes(lowerSearch))
      : tenants;
  }, [data?.tenants, search]);

  const planGroups = useMemo(() => {
    const groupMap = new Map<SubscriptionPlan, FeatureFlagTenantInfo[]>();
    for (const tenant of filtered) {
      const existing = groupMap.get(tenant.plan);
      if (existing) {
        existing.push(tenant);
      } else {
        groupMap.set(tenant.plan, [tenant]);
      }
    }
    const planOrder: Record<SubscriptionPlan, number> = {
      [SubscriptionPlan.Premium]: 0,
      [SubscriptionPlan.Standard]: 1,
      [SubscriptionPlan.Basis]: 2
    };
    return [...groupMap.entries()]
      .sort(([a], [b]) => (planOrder[a] ?? 99) - (planOrder[b] ?? 99))
      .map(([plan, members]) => ({ plan, tenants: members }));
  }, [filtered]);

  const isSearching = search.length > 0;

  return (
    <div className="flex flex-col gap-4">
      <div>
        <h3>
          <Trans>Account status</Trans>
        </h3>
        <p className="text-sm text-muted-foreground">
          <Trans>
            Accounts are automatically enabled or disabled based on their subscription plan. No manual overrides are
            available for plan-managed flags.
          </Trans>
        </p>
      </div>
      <TextField
        name="search"
        placeholder={t`Search by account name or ID`}
        value={search}
        onChange={(value) => setSearch(value)}
        className="max-w-[20rem]"
      />
      {isLoading ? (
        <div className="flex flex-col gap-2">
          <Skeleton className="h-10 w-full rounded-md" />
          <Skeleton className="h-14 w-full rounded-md" />
        </div>
      ) : filtered.length === 0 ? (
        <PlanFeatureFlagTenantsEmpty hasFilters={isSearching} />
      ) : isSearching ? (
        <PlanFeatureFlagTenantTable ariaLabel={t`Search results`} tenants={filtered} />
      ) : (
        planGroups.map((group) => <CollapsiblePlanGroup key={group.plan} plan={group.plan} tenants={group.tenants} />)
      )}
    </div>
  );
}

function PlanFeatureFlagTenantsEmpty({ hasFilters }: Readonly<{ hasFilters: boolean }>) {
  return (
    <Empty>
      <EmptyHeader>
        <EmptyMedia variant="icon">
          <Building2Icon />
        </EmptyMedia>
        <EmptyTitle>
          {hasFilters ? (
            <Trans>No accounts match your filters</Trans>
          ) : (
            <Trans>No accounts qualify for this feature yet</Trans>
          )}
        </EmptyTitle>
        <EmptyDescription>
          {hasFilters ? (
            <Trans>Try clearing the search or filters to see more results.</Trans>
          ) : (
            <Trans>Accounts will appear here as they qualify for this feature.</Trans>
          )}
        </EmptyDescription>
      </EmptyHeader>
    </Empty>
  );
}

function CollapsiblePlanGroup({
  plan,
  tenants
}: Readonly<{ plan: SubscriptionPlan; tenants: FeatureFlagTenantInfo[] }>) {
  const [isOpen, setIsOpen] = useState(true);
  const planLabel = getSubscriptionPlanLabel(plan);

  return (
    <Collapsible open={isOpen} onOpenChange={setIsOpen} className="flex flex-col gap-1">
      <CollapsibleTrigger className="flex cursor-pointer items-center gap-1 text-left">
        <ChevronDown
          className={`size-4 text-muted-foreground transition ${isOpen ? "" : "-rotate-90"}`}
          aria-hidden={true}
        />
        <h4 className="text-muted-foreground">
          {planLabel} ({tenants.length})
        </h4>
      </CollapsibleTrigger>
      <CollapsibleContent>
        <PlanFeatureFlagTenantTable ariaLabel={planLabel} tenants={tenants} />
      </CollapsibleContent>
    </Collapsible>
  );
}
