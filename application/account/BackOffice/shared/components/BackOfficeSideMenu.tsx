import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import {
  Sidebar,
  SidebarContent,
  SidebarGroup,
  SidebarGroupContent,
  SidebarGroupLabel,
  SidebarHeader,
  SidebarMenu,
  SidebarMenuBadge,
  SidebarMenuButton,
  SidebarMenuItem,
  SidebarRail
} from "@repo/ui/components/Sidebar";
import { Link as RouterLink, useRouter } from "@tanstack/react-router";
import {
  BlocksIcon,
  Building2Icon,
  FlagIcon,
  HomeIcon,
  InboxIcon,
  ReceiptIcon,
  UsersIcon,
  ZapIcon
} from "lucide-react";

import { api } from "@/shared/lib/api/client";

import { BackOfficeAvatarMenu } from "./BackOfficeAvatarMenu";

const normalizePath = (path: string): string => path.replace(/\/$/, "") || "/";

const isSubscriptionEnabled = import.meta.runtime_env.PUBLIC_SUBSCRIPTION_ENABLED === "true";

export function BackOfficeSideMenu() {
  const router = useRouter();
  const currentPath = normalizePath(router.state.location.pathname);
  const isAccountsActive = currentPath === "/accounts" || currentPath.startsWith("/accounts/");
  const isUsersActive = currentPath === "/users" || currentPath.startsWith("/users/");
  const isBillingEventsActive = currentPath === "/billing-events" || currentPath.startsWith("/billing-events/");
  const isInvoicesActive = currentPath === "/invoices" || currentPath.startsWith("/invoices/");
  const isFeatureFlagsActive = currentPath === "/feature-flags" || currentPath.startsWith("/feature-flags/");
  const isSupportTicketsActive = currentPath === "/support/tickets" || currentPath.startsWith("/support/tickets/");
  const isComponentsActive = currentPath === "/components" || currentPath.startsWith("/components/");

  const { data: ticketsData } = api.useQuery(
    "get",
    "/api/back-office/support-tickets",
    { params: { query: { PageSize: 1 } } },
    { staleTime: 60_000 }
  );
  const unresolvedCount =
    (ticketsData?.counts.new ?? 0) +
    (ticketsData?.counts.awaitingAgent ?? 0) +
    (ticketsData?.counts.awaitingUser ?? 0) +
    (ticketsData?.counts.awaitingInternal ?? 0);

  return (
    <Sidebar collapsible="icon">
      <nav className="contents" aria-label={t`Main navigation`}>
        <SidebarHeader>
          <BackOfficeAvatarMenu />
        </SidebarHeader>
        <SidebarContent>
          <SidebarGroup>
            <SidebarGroupLabel>
              <Trans>Navigation</Trans>
            </SidebarGroupLabel>
            <SidebarGroupContent>
              <SidebarMenu>
                <SidebarMenuItem>
                  <SidebarMenuButton asChild={true} isActive={currentPath === "/"} tooltip={t`Dashboard`}>
                    <RouterLink to="/">
                      <HomeIcon />
                      <span>
                        <Trans>Dashboard</Trans>
                      </span>
                    </RouterLink>
                  </SidebarMenuButton>
                </SidebarMenuItem>
                <SidebarMenuItem>
                  <SidebarMenuButton asChild={true} isActive={isAccountsActive} tooltip={t`Accounts`}>
                    <RouterLink to="/accounts">
                      <Building2Icon />
                      <span>
                        <Trans>Accounts</Trans>
                      </span>
                    </RouterLink>
                  </SidebarMenuButton>
                </SidebarMenuItem>
                <SidebarMenuItem>
                  <SidebarMenuButton asChild={true} isActive={isUsersActive} tooltip={t`Users`}>
                    <RouterLink to="/users">
                      <UsersIcon />
                      <span>
                        <Trans>Users</Trans>
                      </span>
                    </RouterLink>
                  </SidebarMenuButton>
                </SidebarMenuItem>
              </SidebarMenu>
            </SidebarGroupContent>
          </SidebarGroup>
          {isSubscriptionEnabled && (
            <SidebarGroup>
              <SidebarGroupLabel>
                <Trans>Billing</Trans>
              </SidebarGroupLabel>
              <SidebarGroupContent>
                <SidebarMenu>
                  <SidebarMenuItem>
                    <SidebarMenuButton asChild={true} isActive={isInvoicesActive} tooltip={t`Invoices`}>
                      <RouterLink to="/invoices">
                        <ReceiptIcon />
                        <span>
                          <Trans>Invoices</Trans>
                        </span>
                      </RouterLink>
                    </SidebarMenuButton>
                  </SidebarMenuItem>
                  <SidebarMenuItem>
                    <SidebarMenuButton asChild={true} isActive={isBillingEventsActive} tooltip={t`Billing events`}>
                      <RouterLink to="/billing-events">
                        <ZapIcon />
                        <span>
                          <Trans>Billing events</Trans>
                        </span>
                      </RouterLink>
                    </SidebarMenuButton>
                  </SidebarMenuItem>
                </SidebarMenu>
              </SidebarGroupContent>
            </SidebarGroup>
          )}
          <SupportSidebarGroup isActive={isSupportTicketsActive} unresolvedCount={unresolvedCount} />
          <SidebarGroup>
            <SidebarGroupLabel>
              <Trans>Platform</Trans>
            </SidebarGroupLabel>
            <SidebarGroupContent>
              <SidebarMenu>
                <SidebarMenuItem>
                  <SidebarMenuButton asChild={true} isActive={isFeatureFlagsActive} tooltip={t`Feature flags`}>
                    <RouterLink to="/feature-flags">
                      <FlagIcon />
                      <span>
                        <Trans>Feature flags</Trans>
                      </span>
                    </RouterLink>
                  </SidebarMenuButton>
                </SidebarMenuItem>
              </SidebarMenu>
            </SidebarGroupContent>
          </SidebarGroup>
          <SidebarGroup className="mt-auto">
            <SidebarGroupLabel>
              <Trans>Developer</Trans>
            </SidebarGroupLabel>
            <SidebarGroupContent>
              <SidebarMenu>
                <SidebarMenuItem>
                  <SidebarMenuButton asChild={true} isActive={isComponentsActive} tooltip={t`Components`}>
                    <RouterLink to="/components">
                      <BlocksIcon />
                      <span>
                        <Trans>Components</Trans>
                      </span>
                    </RouterLink>
                  </SidebarMenuButton>
                </SidebarMenuItem>
              </SidebarMenu>
            </SidebarGroupContent>
          </SidebarGroup>
        </SidebarContent>
      </nav>
      <SidebarRail />
    </Sidebar>
  );
}

function SupportSidebarGroup({ isActive, unresolvedCount }: { isActive: boolean; unresolvedCount: number }) {
  return (
    <SidebarGroup>
      <SidebarGroupLabel>
        <Trans>Support</Trans>
      </SidebarGroupLabel>
      <SidebarGroupContent>
        <SidebarMenu>
          <SidebarMenuItem>
            <SidebarMenuButton asChild={true} isActive={isActive} tooltip={t`Tickets`}>
              <RouterLink to="/support/tickets">
                <InboxIcon />
                <span>
                  <Trans>Tickets</Trans>
                </span>
              </RouterLink>
            </SidebarMenuButton>
            {unresolvedCount > 0 && <SidebarMenuBadge>{unresolvedCount}</SidebarMenuBadge>}
          </SidebarMenuItem>
        </SidebarMenu>
      </SidebarGroupContent>
    </SidebarGroup>
  );
}
