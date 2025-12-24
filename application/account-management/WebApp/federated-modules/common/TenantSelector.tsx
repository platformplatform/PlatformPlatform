import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import type { UserInfo } from "@repo/infrastructure/auth/AuthenticationProvider";
import { loggedInPath } from "@repo/infrastructure/auth/constants";
import { useUserInfo } from "@repo/infrastructure/auth/hooks";
import { Badge } from "@repo/ui/components/Badge";
import { Button } from "@repo/ui/components/Button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuGroup,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger
} from "@repo/ui/components/DropdownMenu";
import { collapsedContext, overlayContext } from "@repo/ui/components/SideMenu";
import { TenantLogo } from "@repo/ui/components/TenantLogo";
import { Tooltip, TooltipContent, TooltipTrigger } from "@repo/ui/components/Tooltip";
import { SIDE_MENU_COLLAPSED_WIDTH, SIDE_MENU_DEFAULT_WIDTH } from "@repo/ui/utils/responsive";
import { Check, ChevronDown } from "lucide-react";
import { useContext, useEffect, useState } from "react";
import { useSwitchTenant } from "@/shared/hooks/useSwitchTenant";
import type { components } from "@/shared/lib/api/api.generated";
import { api } from "@/shared/lib/api/client";
import { SwitchingAccountLoader } from "./SwitchingAccountLoader";
import "@repo/ui/tailwind.css";

type TenantInfo = components["schemas"]["TenantInfo"];

interface TenantSelectorProps {
  onShowInvitationDialog?: (tenant: TenantInfo) => void;
  variant?: "default" | "mobile-menu";
}

// Helper function to sort tenants
function sortTenants(tenants: TenantInfo[]): TenantInfo[] {
  return [...tenants].sort((a, b) => {
    // Put unnamed accounts at the end
    if (!a.tenantName && b.tenantName) {
      return 1;
    }
    if (a.tenantName && !b.tenantName) {
      return -1;
    }

    // Both have names or both are unnamed, sort alphabetically
    const nameA = a.tenantName || "";
    const nameB = b.tenantName || "";
    return nameA.localeCompare(nameB);
  });
}

// Custom hook for sidebar width management
function useSidebarWidth(isCollapsed: boolean) {
  const [sidebarWidth, setSidebarWidth] = useState(() => {
    if (isCollapsed) {
      return SIDE_MENU_COLLAPSED_WIDTH;
    }
    const stored = localStorage.getItem("side-menu-size");
    return stored ? Number.parseInt(stored, 10) : SIDE_MENU_DEFAULT_WIDTH;
  });

  useEffect(() => {
    const handleResize = (event: CustomEvent<{ width: number }>) => {
      setSidebarWidth(event.detail.width);
    };

    const handleToggle = (event: CustomEvent<{ isCollapsed: boolean }>) => {
      setSidebarWidth(
        event.detail.isCollapsed
          ? SIDE_MENU_COLLAPSED_WIDTH
          : Number.parseInt(localStorage.getItem("side-menu-size") || String(SIDE_MENU_DEFAULT_WIDTH), 10)
      );
    };

    window.addEventListener("side-menu-resize", handleResize as EventListener);
    window.addEventListener("side-menu-toggle", handleToggle as EventListener);

    return () => {
      window.removeEventListener("side-menu-resize", handleResize as EventListener);
      window.removeEventListener("side-menu-toggle", handleToggle as EventListener);
    };
  }, []);

  // Update sidebar width when collapsed state changes
  useEffect(() => {
    if (isCollapsed) {
      setSidebarWidth(SIDE_MENU_COLLAPSED_WIDTH);
    } else {
      const stored = localStorage.getItem("side-menu-size");
      setSidebarWidth(stored ? Number.parseInt(stored, 10) : SIDE_MENU_DEFAULT_WIDTH);
    }
  }, [isCollapsed]);

  return sidebarWidth;
}

// Helper component for the tenant menu dropdown
function TenantMenuDropdown({
  currentTenantName,
  currentTenantNameForLogo,
  currentTenantLogoUrl,
  newTenantsCount,
  isCollapsed,
  isSwitching,
  variant,
  sidebarWidth,
  sortedTenants,
  currentTenantId,
  userInfo,
  handleTenantSwitch,
  setIsMenuOpen,
  showTooltip = false
}: {
  currentTenantName: string;
  currentTenantNameForLogo: string;
  currentTenantLogoUrl: string | null | undefined;
  newTenantsCount: number;
  isCollapsed: boolean;
  isSwitching: boolean;
  variant: "default" | "mobile-menu";
  sidebarWidth: number;
  sortedTenants: TenantInfo[];
  currentTenantId: string | undefined;
  userInfo: UserInfo | null;
  handleTenantSwitch: (tenant: TenantInfo) => void;
  setIsMenuOpen: (open: boolean) => void;
  showTooltip?: boolean;
}) {
  const triggerButton = (
    <Button
      variant="ghost"
      className={`relative flex h-11 w-full items-center gap-0 overflow-visible rounded-md py-2 pr-2 font-normal text-sm hover:bg-hover-background focus:outline-none focus-visible:ring-2 focus-visible:ring-ring ${isCollapsed ? "pl-2" : "pl-2.5"} `}
    >
      <div className="flex h-8 w-8 shrink-0 items-center justify-center">
        <TenantLogo
          logoUrl={currentTenantLogoUrl}
          tenantName={currentTenantNameForLogo}
          size="xs"
          isRound={false}
          className="shrink-0"
        />
      </div>
      {!isCollapsed && (
        <>
          <div className="ml-4 flex-1 overflow-hidden text-ellipsis whitespace-nowrap text-left font-semibold text-foreground">
            {currentTenantName}
          </div>
          {newTenantsCount > 0 && <div className="ml-2 h-2 w-2 shrink-0 rounded-full bg-warning" />}
          <ChevronDown className="ml-2 h-3.5 w-3.5 shrink-0 text-foreground opacity-70" />
        </>
      )}
    </Button>
  );

  return (
    <div className="relative w-full px-3">
      <div className="">
        <DropdownMenu onOpenChange={setIsMenuOpen}>
          {showTooltip ? (
            <Tooltip>
              <TooltipTrigger render={<DropdownMenuTrigger disabled={isSwitching} render={triggerButton} />} />
              <TooltipContent side="right" sideOffset={4}>
                {currentTenantName}
              </TooltipContent>
            </Tooltip>
          ) : (
            <DropdownMenuTrigger disabled={isSwitching} render={triggerButton} />
          )}
          <DropdownMenuContent
            align={variant === "mobile-menu" ? "end" : "start"}
            side={variant === "mobile-menu" ? "bottom" : isCollapsed ? "right" : "bottom"}
            className="w-auto bg-popover"
            style={{ minWidth: `${Math.max(SIDE_MENU_DEFAULT_WIDTH, sidebarWidth) - 24}px` }}
          >
            <DropdownMenuGroup>
              <DropdownMenuLabel>
                <div className="flex flex-col gap-1 font-semibold text-sm">
                  <Trans>Select Account</Trans>
                </div>
              </DropdownMenuLabel>
            </DropdownMenuGroup>
            <DropdownMenuSeparator />
            {sortedTenants.map((tenant: TenantInfo) => (
              <DropdownMenuItem key={tenant.tenantId} onClick={() => handleTenantSwitch(tenant)}>
                <TenantLogo
                  logoUrl={tenant.logoUrl}
                  tenantName={tenant.tenantName || ""}
                  size="xs"
                  isRound={false}
                  className="shrink-0"
                  style={{ width: "24px", height: "24px" }}
                />
                <div className="flex flex-1 items-center justify-between gap-2">
                  <div className="flex flex-col overflow-hidden">
                    <span className="overflow-hidden text-ellipsis whitespace-nowrap">
                      {tenant.tenantName || t`Unnamed account`}
                    </span>
                    <span className="overflow-hidden text-ellipsis whitespace-nowrap text-muted-foreground text-xs">
                      {userInfo?.email}
                    </span>
                  </div>
                  <div className="flex shrink-0 items-center gap-2">
                    {tenant.isNew && (
                      <Badge variant="secondary" className="bg-warning text-warning-foreground text-xs">
                        <Trans>Invitation pending</Trans>
                      </Badge>
                    )}
                    {tenant.tenantId === currentTenantId && <Check className="h-4 w-4" />}
                  </div>
                </div>
              </DropdownMenuItem>
            ))}
          </DropdownMenuContent>
        </DropdownMenu>
      </div>
    </div>
  );
}

// Helper component for single tenant display
function SingleTenantDisplay({
  currentTenantName,
  currentTenantNameForLogo,
  currentTenantLogoUrl,
  isCollapsed
}: {
  currentTenantName: string;
  currentTenantNameForLogo: string;
  currentTenantLogoUrl: string | null | undefined;
  isCollapsed: boolean;
}) {
  return (
    <div className="relative w-full px-3">
      <div className="">
        <div
          className={`flex h-11 w-full items-center rounded-md py-2 pr-2 text-sm ${isCollapsed ? "pl-2" : "pl-2.5"}`}
        >
          <div className="flex h-8 w-8 shrink-0 items-center justify-center">
            <TenantLogo
              logoUrl={currentTenantLogoUrl}
              tenantName={currentTenantNameForLogo}
              size="xs"
              isRound={false}
              className="shrink-0"
            />
          </div>
          {!isCollapsed && (
            <div className="ml-4 flex-1 overflow-hidden text-ellipsis whitespace-nowrap text-left font-semibold text-foreground">
              {currentTenantName}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}

export default function TenantSelector({ onShowInvitationDialog, variant = "default" }: TenantSelectorProps = {}) {
  const userInfo = useUserInfo();
  const isCollapsed = useContext(collapsedContext);
  const overlayCtx = useContext(overlayContext);
  const [isMenuOpen, setIsMenuOpen] = useState(false);
  const [isSwitching, setIsSwitching] = useState(false);

  // Use custom hook for sidebar width management
  const sidebarWidth = useSidebarWidth(isCollapsed);

  // Dispatch event when menu open state changes
  useEffect(() => {
    window.dispatchEvent(new CustomEvent("tenant-menu-toggle", { detail: { isOpen: isMenuOpen } }));
  }, [isMenuOpen]);

  // Fetch available tenants
  const { data: tenantsResponse, isLoading, refetch } = api.useQuery("get", "/api/account-management/tenants");

  // Listen for tenant updates and refetch
  useEffect(() => {
    const handleTenantUpdate = () => {
      refetch().catch(() => {
        // Handle refetch errors silently
      });
    };

    window.addEventListener("tenant-updated", handleTenantUpdate);
    return () => {
      window.removeEventListener("tenant-updated", handleTenantUpdate);
    };
  }, [refetch]);

  // Use the shared switch tenant hook
  const { switchTenant } = useSwitchTenant({
    onMutate: () => {
      // Show the loader immediately
      setIsSwitching(true);
    },
    onSuccess: () => {
      // Stay on the same path, but default to loggedInPath if on root
      // Don't set isSwitching to false here - let the redirect handle it
      const targetPath = window.location.pathname === "/" ? loggedInPath : window.location.pathname;
      window.location.href = targetPath;
    },
    onError: () => {
      // Hide the loader on error
      setIsSwitching(false);
    }
  });

  if (!userInfo?.isAuthenticated || isLoading) {
    return null;
  }

  const tenants = tenantsResponse?.tenants || [];
  const currentTenantId = userInfo.tenantId;

  // Sort tenants using helper function
  const sortedTenants = sortTenants(tenants);

  // Get tenant name from tenants list to ensure it's always up-to-date
  const currentTenant = tenants.find((t) => t.tenantId === currentTenantId);
  const currentTenantName = currentTenant?.tenantName || userInfo.tenantName || "PlatformPlatform";
  const currentTenantNameForLogo = currentTenant?.tenantName || userInfo.tenantName || "";
  const newTenantsCount = tenants.filter((t) => t.isNew && t.tenantId !== currentTenantId).length;
  // Always use fresh data from API when available, only fall back to userInfo if tenant not found in API response
  const currentTenantLogoUrl = currentTenant ? currentTenant.logoUrl : userInfo.tenantLogoUrl;

  const handleTenantSwitch = (tenant: TenantInfo) => {
    if (tenant.tenantId === currentTenantId) {
      return;
    }

    if (tenant.isNew && onShowInvitationDialog) {
      // Close mobile menu if it's open
      if (overlayCtx?.isOpen) {
        overlayCtx.close();
      }
      // Use small timeout to ensure menu closes before dialog opens
      setTimeout(() => {
        onShowInvitationDialog(tenant);
      }, 100);
    } else if (!tenant.isNew) {
      // Switch directly for existing tenants
      switchTenant(tenant);
    }
  };

  // When there's only one tenant, just show logo and name
  if (tenants.length <= 1) {
    return (
      <SingleTenantDisplay
        currentTenantName={currentTenantName}
        currentTenantNameForLogo={currentTenantNameForLogo}
        currentTenantLogoUrl={currentTenantLogoUrl}
        isCollapsed={isCollapsed}
      />
    );
  }

  // When there are multiple tenants, show dropdown with styled content
  return (
    <>
      <TenantMenuDropdown
        currentTenantName={currentTenantName}
        currentTenantNameForLogo={currentTenantNameForLogo}
        currentTenantLogoUrl={currentTenantLogoUrl}
        newTenantsCount={newTenantsCount}
        isCollapsed={isCollapsed}
        isSwitching={isSwitching}
        variant={variant}
        sidebarWidth={sidebarWidth}
        sortedTenants={sortedTenants}
        currentTenantId={currentTenantId}
        userInfo={userInfo}
        handleTenantSwitch={handleTenantSwitch}
        setIsMenuOpen={setIsMenuOpen}
        showTooltip={isCollapsed}
      />
      {isSwitching && <SwitchingAccountLoader />}
    </>
  );
}
