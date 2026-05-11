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
  SidebarMenuButton,
  SidebarMenuItem,
  SidebarRail
} from "@repo/ui/components/Sidebar";
import { Link as RouterLink, useRouter } from "@tanstack/react-router";
import { Building2Icon, HomeIcon, ReceiptIcon, UsersIcon, ZapIcon } from "lucide-react";

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
        </SidebarContent>
      </nav>
      <SidebarRail />
    </Sidebar>
  );
}
