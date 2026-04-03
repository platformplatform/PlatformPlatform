import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { AppLayout } from "@repo/ui/components/AppLayout";
import { Badge } from "@repo/ui/components/Badge";
import { SidebarInset, SidebarProvider } from "@repo/ui/components/Sidebar";
import { Skeleton } from "@repo/ui/components/Skeleton";
import { Switch } from "@repo/ui/components/Switch";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@repo/ui/components/Table";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@repo/ui/components/Tabs";
import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { useMemo, useState } from "react";
import { toast } from "sonner";

import { BackOfficeSideMenu } from "@/shared/components/BackOfficeSideMenu";
import { api } from "@/shared/lib/api/client";

import type { FeatureFlagInfo, GetFeatureFlagsResponse } from "./-components/types";

type TabFilter = "all" | "Tenant" | "User" | "System";

export const Route = createFileRoute("/feature-flags/")({
  staticData: { trackingTitle: "Feature flags" },
  component: FeatureFlagsPage
});

export default function FeatureFlagsPage() {
  const [activeTab, setActiveTab] = useState<TabFilter>("all");
  const { data, isLoading } = api.useQuery("get", "/api/back-office/feature-flags") as {
    data: GetFeatureFlagsResponse | undefined;
    isLoading: boolean;
  };

  const filteredFlags = useMemo(() => {
    if (!data?.flags) return [];
    if (activeTab === "all") return data.flags;
    return data.flags.filter((flag) => flag.scope === activeTab);
  }, [data?.flags, activeTab]);

  return (
    <SidebarProvider>
      <BackOfficeSideMenu />
      <SidebarInset>
        <AppLayout title={t`Feature flags`} subtitle={t`Manage feature flags across the platform.`}>
          <Tabs
            value={activeTab}
            onValueChange={(value) => setActiveTab(value as TabFilter)}
            className="relative z-10 mb-4"
          >
            <TabsList aria-label={t`Filter by flag type`}>
              <TabsTrigger value="all">
                <Trans>All</Trans>
              </TabsTrigger>
              <TabsTrigger value="Tenant">
                <Trans>Tenant</Trans>
              </TabsTrigger>
              <TabsTrigger value="User">
                <Trans>User</Trans>
              </TabsTrigger>
              <TabsTrigger value="System">
                <Trans>System</Trans>
              </TabsTrigger>
            </TabsList>

            <TabsContent value={activeTab}>
              {isLoading ? <FeatureFlagsSkeleton /> : <FeatureFlagTable flags={filteredFlags} />}
            </TabsContent>
          </Tabs>
        </AppLayout>
      </SidebarInset>
    </SidebarProvider>
  );
}

function FeatureFlagTable({ flags }: Readonly<{ flags: FeatureFlagInfo[] }>) {
  const navigate = useNavigate();

  return (
    <div className="rounded-md border">
      <Table rowSize="compact" aria-label={t`Feature flags`}>
        <TableHeader>
          <TableRow>
            <TableHead>
              <Trans>Flag name</Trans>
            </TableHead>
            <TableHead>
              <Trans>Type</Trans>
            </TableHead>
            <TableHead>
              <Trans>Rollout</Trans>
            </TableHead>
            <TableHead className="text-right">
              <Trans>Status</Trans>
            </TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {flags.map((flag) => (
            <FeatureFlagRow
              key={flag.key}
              flag={flag}
              onClick={() => {
                if (flag.scope === "Tenant") {
                  navigate({ to: "/feature-flags/$flagKey", params: { flagKey: flag.key } });
                }
              }}
            />
          ))}
        </TableBody>
      </Table>
    </div>
  );
}

function FeatureFlagRow({
  flag,
  onClick
}: Readonly<{
  flag: FeatureFlagInfo;
  onClick: () => void;
}>) {
  const activateMutation = api.useMutation("put", "/api/back-office/feature-flags/{flagKey}/activate");
  const deactivateMutation = api.useMutation("put", "/api/back-office/feature-flags/{flagKey}/deactivate");
  const isPending = activateMutation.isPending || deactivateMutation.isPending;

  const handleToggle = (checked: boolean) => {
    if (flag.scope === "System") return;

    const mutation = checked ? activateMutation : deactivateMutation;
    mutation.mutate(
      { params: { path: { flagKey: flag.key } } },
      {
        onSuccess: () => {
          toast.success(checked ? t`Feature flag activated` : t`Feature flag deactivated`);
        }
      }
    );
  };

  const isTenantScoped = flag.scope === "Tenant";

  return (
    <TableRow className={isTenantScoped ? "cursor-pointer" : undefined} onClick={isTenantScoped ? onClick : undefined}>
      <TableCell className="font-medium">{flag.description}</TableCell>
      <TableCell>
        <Badge variant="secondary">{flag.scope}</Badge>
      </TableCell>
      <TableCell>
        {flag.rolloutPercentage !== null ? (
          `${flag.rolloutPercentage}%`
        ) : (
          <span className="text-muted-foreground">--</span>
        )}
      </TableCell>
      <TableCell className="text-right">
        {flag.scope === "System" ? (
          <Badge variant={flag.isActive ? "default" : "outline"}>{flag.isActive ? t`Active` : t`Inactive`}</Badge>
        ) : (
          <Switch
            checked={flag.isActive}
            onCheckedChange={handleToggle}
            disabled={isPending}
            aria-label={t`Toggle ${flag.description}`}
            onClick={(event) => event.stopPropagation()}
          />
        )}
      </TableCell>
    </TableRow>
  );
}

function FeatureFlagsSkeleton() {
  return (
    <div className="flex flex-col gap-2">
      <Skeleton className="h-10 w-full rounded-md" />
      <Skeleton className="h-14 w-full rounded-md" />
      <Skeleton className="h-14 w-full rounded-md" />
      <Skeleton className="h-14 w-full rounded-md" />
    </div>
  );
}
