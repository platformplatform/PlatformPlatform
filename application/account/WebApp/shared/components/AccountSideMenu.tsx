import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { useUserInfo } from "@repo/infrastructure/auth/hooks";
import { useFeatureFlag } from "@repo/infrastructure/featureFlags/useFeatureFlag";
import {
  collapsedContext,
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
import {
  Building2Icon,
  CreditCardIcon,
  HomeIcon,
  MonitorSmartphoneIcon,
  SlidersHorizontalIcon,
  UserIcon,
  UsersIcon
} from "lucide-react";
import { use } from "react";

import MobileMenu from "@/federated-modules/sideMenu/MobileMenu";
import UserMenu from "@/federated-modules/userMenu/UserMenu";
import { useMainNavigation } from "@/shared/hooks/useMainNavigation";

const normalizePath = (path: string): string => path.replace(/\/$/, "") || "/";

function HeaderUserMenu() {
  // Federated UserMenu reads `collapsedContext` (shimmed by SidebarProvider in new Sidebar).
  const isCollapsed = use(collapsedContext);
  return <UserMenu isCollapsed={isCollapsed} />;
}

export function AccountSideMenu() {
  const userInfo = useUserInfo();
  const router = useRouter();
  const currentPath = normalizePath(router.state.location.pathname);
  const { navigateToMain } = useMainNavigation();
  const { enabled: isSubscriptionEnabled } = useFeatureFlag("subscriptions");
  const { enabled: isAccountOverviewEnabled } = useFeatureFlag("account-overview");

  const isActive = (target: string, matchPrefix = false) => {
    const normalized = normalizePath(target);
    return matchPrefix ? currentPath.startsWith(normalized) : currentPath === normalized;
  };

  const showBilling = userInfo?.role === "Owner" && isSubscriptionEnabled;

  return (
    <Sidebar collapsible="icon" mobileContent={<MobileMenu onNavigate={navigateToMain ?? undefined} />}>
      <nav className="contents" aria-label={t`Main navigation`}>
        <SidebarHeader>
          <HeaderUserMenu />
        </SidebarHeader>
        <SidebarContent>
          <SidebarGroup>
            <SidebarGroupLabel>
              <Trans>User</Trans>
            </SidebarGroupLabel>
            <SidebarGroupContent>
              <SidebarMenu>
                <SidebarMenuItem>
                  <SidebarMenuButton asChild={true} isActive={isActive("/user/profile")} tooltip={t`User profile`}>
                    <RouterLink to="/user/profile" aria-label={t`User profile`}>
                      <UserIcon />
                      <span>
                        <Trans>Profile</Trans>
                      </span>
                    </RouterLink>
                  </SidebarMenuButton>
                </SidebarMenuItem>
                <SidebarMenuItem>
                  <SidebarMenuButton
                    asChild={true}
                    isActive={isActive("/user/preferences")}
                    tooltip={t`User preferences`}
                  >
                    <RouterLink to="/user/preferences" aria-label={t`User preferences`}>
                      <SlidersHorizontalIcon />
                      <span>
                        <Trans>Preferences</Trans>
                      </span>
                    </RouterLink>
                  </SidebarMenuButton>
                </SidebarMenuItem>
                <SidebarMenuItem>
                  <SidebarMenuButton asChild={true} isActive={isActive("/user/sessions")} tooltip={t`User sessions`}>
                    <RouterLink to="/user/sessions" aria-label={t`User sessions`}>
                      <MonitorSmartphoneIcon />
                      <span>
                        <Trans>Sessions</Trans>
                      </span>
                    </RouterLink>
                  </SidebarMenuButton>
                </SidebarMenuItem>
              </SidebarMenu>
            </SidebarGroupContent>
          </SidebarGroup>

          <SidebarGroup>
            <SidebarGroupLabel>
              <Trans>Account</Trans>
            </SidebarGroupLabel>
            <SidebarGroupContent>
              <SidebarMenu>
                {isAccountOverviewEnabled && (
                  <SidebarMenuItem>
                    <SidebarMenuButton asChild={true} isActive={isActive("/account")} tooltip={t`Overview`}>
                      <RouterLink to="/account">
                        <HomeIcon />
                        <span>
                          <Trans>Overview</Trans>
                        </span>
                      </RouterLink>
                    </SidebarMenuButton>
                  </SidebarMenuItem>
                )}
                <SidebarMenuItem>
                  <SidebarMenuButton asChild={true} isActive={isActive("/account/settings")} tooltip={t`Settings`}>
                    <RouterLink to="/account/settings">
                      <Building2Icon />
                      <span>
                        <Trans>Settings</Trans>
                      </span>
                    </RouterLink>
                  </SidebarMenuButton>
                </SidebarMenuItem>
                <SidebarMenuItem>
                  <SidebarMenuButton asChild={true} isActive={isActive("/account/users", true)} tooltip={t`Users`}>
                    <RouterLink to="/account/users">
                      <UsersIcon />
                      <span>
                        <Trans>Users</Trans>
                      </span>
                    </RouterLink>
                  </SidebarMenuButton>
                </SidebarMenuItem>
                {showBilling && (
                  <SidebarMenuItem>
                    <SidebarMenuButton
                      asChild={true}
                      isActive={isActive("/account/billing", true)}
                      tooltip={t`Billing`}
                    >
                      <RouterLink to="/account/billing">
                        <CreditCardIcon />
                        <span>
                          <Trans>Billing</Trans>
                        </span>
                      </RouterLink>
                    </SidebarMenuButton>
                  </SidebarMenuItem>
                )}
              </SidebarMenu>
            </SidebarGroupContent>
          </SidebarGroup>
        </SidebarContent>
      </nav>
      <SidebarRail />
    </Sidebar>
  );
}
