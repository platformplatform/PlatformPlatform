import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import {
  Sidebar,
  SidebarContent,
  SidebarFooter,
  SidebarGroup,
  SidebarGroupContent,
  SidebarGroupLabel,
  SidebarHeader,
  SidebarInset,
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
  SidebarProvider
} from "@repo/ui/components/Sidebar";
import {
  Building2Icon,
  HomeIcon,
  MonitorSmartphoneIcon,
  SettingsIcon,
  SlidersHorizontalIcon,
  UserIcon,
  UsersIcon
} from "lucide-react";

// Preview uses collapsible="none" so the sidebar sits in normal flow inside a bounded box.
// The full interactive sidebar (drag-to-resize, hover-reveal toggle, icon collapse, mobile Sheet)
// renders as the real sidemenu in Back-Office, Main, and Account once those SCSs are migrated.
export function SidebarPreview() {
  return (
    <div className="flex flex-col gap-4">
      <p className="text-sm text-muted-foreground">
        <Trans>
          Static preview of the Sidebar component. The fully interactive version — with drag-to-resize, hover-reveal
          toggle, and icon collapse — renders as the real sidemenu in each self-contained system.
        </Trans>
      </p>
      <div className="relative flex h-[30rem] overflow-hidden rounded-lg border bg-background">
        <SidebarProvider defaultOpen={true} className="h-full min-h-0!">
          <Sidebar collapsible="none" className="h-full border-r">
            <SidebarHeader>
              <div className="flex items-center gap-2 px-2 py-1.5 text-sm font-semibold">
                <Building2Icon className="size-5 shrink-0" />
                <span className="group-data-[collapsible=icon]:hidden">
                  <Trans>Preview app</Trans>
                </span>
              </div>
            </SidebarHeader>
            <SidebarContent>
              <SidebarGroup>
                <SidebarGroupLabel>
                  <Trans>User</Trans>
                </SidebarGroupLabel>
                <SidebarGroupContent>
                  <SidebarMenu>
                    <SidebarMenuItem>
                      <SidebarMenuButton tooltip={t`Profile`} isActive={true}>
                        <UserIcon />
                        <span>
                          <Trans>Profile</Trans>
                        </span>
                      </SidebarMenuButton>
                    </SidebarMenuItem>
                    <SidebarMenuItem>
                      <SidebarMenuButton tooltip={t`Preferences`}>
                        <SlidersHorizontalIcon />
                        <span>
                          <Trans>Preferences</Trans>
                        </span>
                      </SidebarMenuButton>
                    </SidebarMenuItem>
                    <SidebarMenuItem>
                      <SidebarMenuButton tooltip={t`Sessions`}>
                        <MonitorSmartphoneIcon />
                        <span>
                          <Trans>Sessions</Trans>
                        </span>
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
                    <SidebarMenuItem>
                      <SidebarMenuButton tooltip={t`Overview`}>
                        <HomeIcon />
                        <span>
                          <Trans>Overview</Trans>
                        </span>
                      </SidebarMenuButton>
                    </SidebarMenuItem>
                    <SidebarMenuItem>
                      <SidebarMenuButton tooltip={t`Users`}>
                        <UsersIcon />
                        <span>
                          <Trans>Users</Trans>
                        </span>
                      </SidebarMenuButton>
                    </SidebarMenuItem>
                    <SidebarMenuItem>
                      <SidebarMenuButton tooltip={t`Settings`}>
                        <SettingsIcon />
                        <span>
                          <Trans>Settings</Trans>
                        </span>
                      </SidebarMenuButton>
                    </SidebarMenuItem>
                  </SidebarMenu>
                </SidebarGroupContent>
              </SidebarGroup>
            </SidebarContent>
            <SidebarFooter>
              <div className="px-2 py-1.5 text-xs text-muted-foreground">
                <Trans>Sidebar footer</Trans>
              </div>
            </SidebarFooter>
          </Sidebar>
          <SidebarInset className="h-full overflow-auto p-6">
            <h3>
              <Trans>Main content</Trans>
            </h3>
            <p className="mt-2 text-sm text-muted-foreground">
              <Trans>
                In the real app the sidebar is fixed-positioned, persists its state and width in localStorage
                (rem-based, not cookies), and exposes a floating chevron to toggle icon collapse plus drag-to-resize
                between 14rem and 25rem.
              </Trans>
            </p>
          </SidebarInset>
        </SidebarProvider>
      </div>
    </div>
  );
}
