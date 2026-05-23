import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import {
  SidebarGroup,
  SidebarGroupContent,
  SidebarGroupLabel,
  SidebarMenu,
  SidebarMenuBadge,
  SidebarMenuButton,
  SidebarMenuItem
} from "@repo/ui/components/Sidebar";
import { Link as RouterLink } from "@tanstack/react-router";
import { InboxIcon } from "lucide-react";

export function SupportSidebarGroup({ isActive, unresolvedCount }: { isActive: boolean; unresolvedCount: number }) {
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
