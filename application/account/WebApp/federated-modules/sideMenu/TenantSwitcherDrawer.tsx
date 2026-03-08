import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Badge } from "@repo/ui/components/Badge";
import { Button } from "@repo/ui/components/Button";
import { Dialog, DialogBody, DialogContent, DialogHeader, DialogTitle } from "@repo/ui/components/Dialog";
import { TenantLogo } from "@repo/ui/components/TenantLogo";
import { Check } from "lucide-react";

import { sortTenants, type TenantInfo } from "../common/tenantUtils";

export function TenantSwitcherDrawer({
  isOpen,
  onOpenChange,
  tenants,
  currentTenantId,
  onTenantSwitch
}: {
  isOpen: boolean;
  onOpenChange: (open: boolean) => void;
  tenants: TenantInfo[];
  currentTenantId: string | undefined;
  onTenantSwitch: (tenant: TenantInfo) => void;
}) {
  const sortedTenants = sortTenants(tenants);

  return (
    <Dialog open={isOpen} onOpenChange={onOpenChange} modal={false} trackingTitle="Switch account">
      <DialogContent
        className="top-auto bottom-0 h-auto max-h-[70dvh] translate-y-0 rounded-t-2xl sm:top-auto sm:bottom-0 sm:max-h-[70dvh] sm:-translate-y-0 sm:rounded-t-2xl sm:rounded-b-none"
        showCloseButton={false}
      >
        <DialogHeader>
          <DialogTitle>
            <Trans>Select account</Trans>
          </DialogTitle>
        </DialogHeader>
        <DialogBody>
          <div className="flex flex-col gap-1">
            {sortedTenants.map((tenant) => (
              <Button
                key={tenant.tenantId}
                variant="ghost"
                onClick={() => onTenantSwitch(tenant)}
                disabled={tenant.tenantId === currentTenantId || tenant.isNew}
                className="flex h-[var(--control-height)] w-full items-center justify-start gap-3 rounded-md px-3 py-2 text-sm font-normal hover:bg-hover-background active:bg-hover-background disabled:cursor-default disabled:opacity-100"
              >
                <TenantLogo logoUrl={tenant.logoUrl} tenantName={tenant.tenantName || ""} />
                <div className="flex min-w-0 flex-1 items-center justify-between gap-2">
                  <span className="overflow-hidden text-left text-ellipsis whitespace-nowrap">
                    {tenant.tenantName || t`Unnamed account`}
                  </span>
                  <div className="flex shrink-0 items-center gap-2">
                    {tenant.isNew && (
                      <Badge variant="secondary" className="bg-warning text-xs text-warning-foreground">
                        <Trans>Invitation pending</Trans>
                      </Badge>
                    )}
                    {tenant.tenantId === currentTenantId && <Check className="size-4" />}
                  </div>
                </div>
              </Button>
            ))}
          </div>
        </DialogBody>
      </DialogContent>
    </Dialog>
  );
}
