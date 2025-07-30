import logoMarkUrl from "@/shared/images/logo-mark.svg";
import type { components } from "@/shared/lib/api/api.generated";
import { api } from "@/shared/lib/api/client";
import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { useUserInfo } from "@repo/infrastructure/auth/hooks";
import { Button } from "@repo/ui/components/Button";
import { Menu, MenuHeader, MenuItem, MenuSeparator, MenuTrigger } from "@repo/ui/components/Menu";
import { collapsedContext } from "@repo/ui/components/SideMenu";
import { TenantLogo } from "@repo/ui/components/TenantLogo";
import { Tooltip, TooltipTrigger } from "@repo/ui/components/Tooltip";
import { Check, ChevronDown, Loader2 } from "lucide-react";
import { useContext } from "react";
import "@repo/ui/tailwind.css";

type TenantInfo = components["schemas"]["TenantInfo"];

export default function TenantSelector() {
  const userInfo = useUserInfo();
  const isCollapsed = useContext(collapsedContext);

  // Fetch available tenants
  const { data: tenantsResponse, isLoading } = api.useQuery("get", "/api/account-management/authentication/tenants");

  // Switch tenant mutation
  const switchTenantMutation = api.useMutation("post", "/api/account-management/authentication/switch-tenant", {
    onSuccess: () => {
      // Redirect to logged-in path after successful switch
      window.location.href = "/";
    }
  });

  if (!userInfo?.isAuthenticated || isLoading) {
    return null;
  }

  const tenants = tenantsResponse?.tenants || [];
  const currentTenantId = userInfo.tenantId;
  const currentTenantName = userInfo.tenantName || "PlatformPlatform";
  const currentTenant = tenants.find(t => t.tenantId === currentTenantId);
  const currentTenantLogoUrl = userInfo.tenantLogoUrl || currentTenant?.logoUrl;

  const handleTenantSwitch = (tenantId: string) => {
    if (tenantId !== currentTenantId) {
      localStorage.setItem(`preferred-tenant-${userInfo.email}`, tenantId);

      switchTenantMutation.mutate({ body: { tenantId } });
    }
  };

  // When there's only one tenant, just show logo and name
  if (tenants.length <= 1) {
    return (
      <div className="w-full px-3">
        <div className={isCollapsed ? "flex w-full justify-center" : "flex w-full items-center gap-3"}>
          <TenantLogo
            logoUrl={currentTenantLogoUrl}
            tenantName={currentTenantName}
            size="xs"
            isRound={false}
            className="shrink-0"
          />
          {!isCollapsed && (
            <>
              <span className="flex-1 overflow-hidden text-ellipsis whitespace-nowrap text-left font-semibold text-foreground text-sm">
                {currentTenantName}
              </span>
              <div className="w-3.5 shrink-0" /> {/* Spacer to match dropdown arrow width */}
            </>
          )}
        </div>
      </div>
    );
  }

  // When there are multiple tenants, show dropdown with styled content
  const menuContent = (
    <div className="relative w-full">
      <div className="-mt-2 mx-2">
        <MenuTrigger>
          <Button
            variant="ghost"
            className="mt-2 h-auto w-full p-4 hover:bg-transparent focus-visible:ring-2 focus-visible:ring-inset"
            isDisabled={switchTenantMutation.isPending}
          >
            <div className={isCollapsed ? "flex w-full justify-center" : "flex w-full items-center gap-3"}>
              <TenantLogo
                logoUrl={currentTenantLogoUrl}
                tenantName={currentTenantName}
                size="xs"
                isRound={false}
                className="shrink-0"
              />
              {!isCollapsed && (
                <>
                  <span className="flex-1 overflow-hidden text-ellipsis whitespace-nowrap text-left font-semibold text-foreground text-sm">
                    {currentTenantName}
                  </span>
                  <ChevronDown className="h-3.5 w-3.5 shrink-0 text-muted-foreground opacity-70" />
                </>
              )}
            </div>
          </Button>
          <Menu placement={isCollapsed ? "right" : "bottom start"}>
            <MenuHeader>
              <div className="flex flex-col gap-1 font-semibold text-sm">
                <Trans>Select Account</Trans>
              </div>
            </MenuHeader>
            <MenuSeparator />
            {tenants.map((tenant: TenantInfo) => (
              <MenuItem
                key={tenant.tenantId}
                id={tenant.tenantId}
                onAction={() => handleTenantSwitch(tenant.tenantId)}
                className="gap-3"
              >
                <TenantLogo
                  logoUrl={tenant.logoUrl}
                  tenantName={tenant.tenantName || ""}
                  size="xs"
                  isRound={false}
                  className="shrink-0"
                  style={{ width: '24px', height: '24px' }}
                />
                <div className="flex min-w-0 flex-1 items-center justify-between gap-2">
                  <span className="overflow-hidden text-ellipsis whitespace-nowrap">
                    {tenant.tenantName || t`Unnamed Account`}
                  </span>
                  {tenant.tenantId === currentTenantId && <Check className="h-4 w-4 shrink-0 text-primary" />}
                </div>
              </MenuItem>
            ))}
          </Menu>
        </MenuTrigger>
      </div>
    </div>
  );

  // Wrap in tooltip for collapsed state
  if (isCollapsed) {
    return (
      <>
        <TooltipTrigger>
          {menuContent}
          <Tooltip placement="right" offset={4}>
            {currentTenantName}
          </Tooltip>
        </TooltipTrigger>
        {switchTenantMutation.isPending && (
          <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
            <div className="flex flex-col items-center gap-4 rounded-lg bg-background p-6">
              <Loader2 className="h-8 w-8 animate-spin text-primary" />
              <p className="text-sm">
                <Trans>Switching account...</Trans>
              </p>
            </div>
          </div>
        )}
      </>
    );
  }

  return (
    <>
      {menuContent}
      {switchTenantMutation.isPending && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
          <div className="flex flex-col items-center gap-4 rounded-lg bg-background p-6">
            <Loader2 className="h-8 w-8 animate-spin text-primary" />
            <p className="text-sm">
              <Trans>Switching account...</Trans>
            </p>
          </div>
        </div>
      )}
    </>
  );
}
