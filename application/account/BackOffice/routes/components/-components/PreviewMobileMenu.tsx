import { Trans } from "@lingui/react/macro";
import { Avatar, AvatarFallback } from "@repo/ui/components/Avatar";
import { Button } from "@repo/ui/components/Button";
import {
  SidebarContent,
  SidebarGroup,
  SidebarGroupContent,
  SidebarMenu,
  SidebarMenuItem
} from "@repo/ui/components/Sidebar";
import { TenantLogo } from "@repo/ui/components/TenantLogo";
import { ChevronDownIcon } from "lucide-react";
import { useState } from "react";

import { PreviewLanguageFlyout, PreviewThemeFlyout, PreviewZoomFlyout } from "./PreviewSettingsFlyouts";

// Mirrors MobileMenu's structure: tenant header block + user-style accordion + navigation. Since
// /components is a preview showcase (no real tenant/user), renders a static accordion with
// Theme/Language/Zoom sub items — each opening a right-side popover flyout with its options.
export function PreviewMobileMenu({ children }: { children: React.ReactNode }) {
  const [isPreviewExpanded, setIsPreviewExpanded] = useState(false);
  return (
    <div className="flex h-full flex-col">
      <div className="mb-2 flex items-center justify-center gap-3 bg-muted px-3 py-2.5 dark:bg-transparent">
        <TenantLogo logoUrl={null} tenantName="PlatformPlatform" size="sm" />
        <h5 className="min-w-0 overflow-hidden font-normal text-ellipsis whitespace-nowrap">
          <Trans>PlatformPlatform</Trans>
        </h5>
      </div>
      <SidebarContent>
        <SidebarGroup>
          <SidebarGroupContent>
            <div className="mx-1">
              <Button
                variant="ghost"
                onClick={() => setIsPreviewExpanded(!isPreviewExpanded)}
                className="flex h-14 w-full items-center justify-start gap-3 rounded-md py-2 pr-3 pl-2 text-sm font-normal hover:bg-hover-background active:bg-hover-background"
                aria-expanded={isPreviewExpanded}
              >
                <Avatar className="size-8">
                  <AvatarFallback className="text-xs">PP</AvatarFallback>
                </Avatar>
                <div className="min-w-0 flex-1 text-left">
                  <div className="truncate font-medium text-foreground">
                    <Trans>Preview settings</Trans>
                  </div>
                  <div className="truncate text-xs text-muted-foreground">preview@platformplatform.net</div>
                </div>
                <ChevronDownIcon
                  className={`size-4 shrink-0 text-muted-foreground transition-transform duration-150 ${isPreviewExpanded ? "rotate-180" : ""}`}
                />
              </Button>
              {isPreviewExpanded && (
                <SidebarMenu>
                  <SidebarMenuItem>
                    <PreviewThemeFlyout />
                  </SidebarMenuItem>
                  <SidebarMenuItem>
                    <PreviewLanguageFlyout />
                  </SidebarMenuItem>
                  <SidebarMenuItem>
                    <PreviewZoomFlyout />
                  </SidebarMenuItem>
                </SidebarMenu>
              )}
            </div>
          </SidebarGroupContent>
        </SidebarGroup>
        {children}
      </SidebarContent>
    </div>
  );
}
