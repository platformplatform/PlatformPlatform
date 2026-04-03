import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Badge } from "@repo/ui/components/Badge";
import { Switch } from "@repo/ui/components/Switch";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@repo/ui/components/Table";
import { toast } from "sonner";

import { api } from "@/shared/lib/api/client";

import type { FlagTenantInfo } from "./types";

export function TenantOverridesSection({
  flagKey,
  tenants
}: Readonly<{
  flagKey: string;
  tenants: FlagTenantInfo[];
}>) {
  return (
    <div className="flex flex-col gap-4">
      <h3>
        <Trans>Tenant overrides</Trans>
      </h3>
      <div className="rounded-md border">
        <Table rowSize="compact" aria-label={t`Tenant overrides`}>
          <TableHeader>
            <TableRow>
              <TableHead>
                <Trans>Tenant</Trans>
              </TableHead>
              <TableHead>
                <Trans>Status</Trans>
              </TableHead>
              <TableHead>
                <Trans>Source</Trans>
              </TableHead>
              <TableHead className="text-right">
                <Trans>Override</Trans>
              </TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {tenants.map((tenant) => (
              <TenantOverrideRow key={tenant.tenantId} flagKey={flagKey} tenant={tenant} />
            ))}
          </TableBody>
        </Table>
      </div>
    </div>
  );
}

function TenantOverrideRow({
  flagKey,
  tenant
}: Readonly<{
  flagKey: string;
  tenant: FlagTenantInfo;
}>) {
  const overrideMutation = api.useMutation("put", "/api/back-office/feature-flags/{flagKey}/tenant-override");

  const handleToggle = (checked: boolean) => {
    overrideMutation.mutate(
      {
        params: { path: { flagKey } },
        body: { tenantId: tenant.tenantId, enabled: checked }
      },
      {
        onSuccess: () => {
          toast.success(t`Tenant override updated`);
        }
      }
    );
  };

  const sourceLabel = getSourceLabel(tenant.source);

  return (
    <TableRow>
      <TableCell className="font-medium">{tenant.tenantName}</TableCell>
      <TableCell>
        <Badge variant={tenant.isEnabled ? "default" : "outline"}>{tenant.isEnabled ? t`Enabled` : t`Disabled`}</Badge>
      </TableCell>
      <TableCell>
        <span className="text-sm text-muted-foreground">{sourceLabel}</span>
      </TableCell>
      <TableCell className="text-right">
        <Switch
          checked={tenant.isEnabled}
          onCheckedChange={handleToggle}
          disabled={overrideMutation.isPending}
          aria-label={t`Override for ${tenant.tenantName}`}
        />
      </TableCell>
    </TableRow>
  );
}

function getSourceLabel(source: string): string {
  switch (source) {
    case "manual_override":
      return t`Manual override`;
    case "ab_rollout":
      return t`A/B rollout`;
    case "default":
      return t`Default`;
    default:
      return source;
  }
}
