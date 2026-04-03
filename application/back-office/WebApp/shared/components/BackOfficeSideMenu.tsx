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
import { BoxIcon, FlagIcon } from "lucide-react";

import logoMark from "@/shared/images/logo-mark.svg";

const normalizePath = (path: string): string => path.replace(/\/$/, "") || "/";

export function BackOfficeSideMenu() {
  const router = useRouter();
  const currentPath = normalizePath(router.state.location.pathname);
  const isActive = (target: string, matchPrefix = false) => {
    const normalized = normalizePath(target);
    return matchPrefix ? currentPath.startsWith(normalized) : currentPath === normalized;
  };

  return (
    <Sidebar collapsible="icon">
      <nav className="contents" aria-label={t`Main navigation`}>
        <SidebarHeader>
          <div className="flex items-center gap-3 pl-[0.875rem] text-sm font-semibold">
            <img className="size-9 shrink-0" src={logoMark} alt={t`PlatformPlatform logo`} />
            <span className="truncate group-data-[collapsible=icon]:hidden">PlatformPlatform</span>
          </div>
        </SidebarHeader>
        <SidebarContent>
          <SidebarGroup>
            <SidebarGroupLabel>
              <Trans>Navigation</Trans>
            </SidebarGroupLabel>
            <SidebarGroupContent>
              <SidebarMenu>
                <SidebarMenuItem>
                  <SidebarMenuButton asChild={true} isActive={isActive("/back-office")} tooltip={t`Dashboard`}>
                    <RouterLink to="/back-office">
                      <BoxIcon />
                      <span>
                        <Trans>Dashboard</Trans>
                      </span>
                    </RouterLink>
                  </SidebarMenuButton>
                </SidebarMenuItem>
                <SidebarMenuItem>
                  <SidebarMenuButton
                    asChild={true}
                    isActive={isActive("/back-office/feature-flags", true)}
                    tooltip={t`Feature flags`}
                  >
                    <RouterLink to="/back-office/feature-flags">
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
        </SidebarContent>
      </nav>
      <SidebarRail />
    </Sidebar>
  );
}
