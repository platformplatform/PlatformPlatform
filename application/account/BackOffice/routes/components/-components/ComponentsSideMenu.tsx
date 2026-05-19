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
  SidebarMenuCollapsibleProvider,
  SidebarMenuItem,
  SidebarRail
} from "@repo/ui/components/Sidebar";
import { Link as RouterLink, useRouter } from "@tanstack/react-router";
import { BlocksIcon, LayersIcon } from "lucide-react";

import { CollapsibleMenu, useHash } from "./ComponentsCollapsibleMenu";
import { PreviewAvatarMenu } from "./PreviewAvatarMenu";
import { PreviewMobileMenu } from "./PreviewMobileMenu";
import {
  chartsIcon as ChartsIcon,
  chartsLabel,
  componentsSections,
  emailsIcon as EmailsIcon,
  emailsLabel,
  examplesSections
} from "./previewSections";

const normalizePath = (path: string): string => path.replace(/\/$/, "") || "/";

export function ComponentsSideMenu() {
  const router = useRouter();
  const currentPath = normalizePath(router.state.location.pathname);
  const isComponentsPage = currentPath === "/components";
  const isExamplesPage = currentPath === "/components/examples";
  const isChartsPage = currentPath === "/components/charts";
  const isEmailsPage = currentPath === "/components/emails";

  const componentsHash = useHash("controls");
  const examplesHash = useHash("dialogs");

  const renderNavigationGroup = () => (
    <SidebarGroup>
      <SidebarGroupLabel>
        <Trans>Navigation</Trans>
      </SidebarGroupLabel>
      <SidebarGroupContent>
        <SidebarMenu>
          <SidebarMenuCollapsibleProvider
            defaultExpanded={isComponentsPage ? "components" : isExamplesPage ? "examples" : null}
          >
            <CollapsibleMenu
              groupKey="components"
              icon={BlocksIcon}
              label={<Trans>Components</Trans>}
              collapseLabel={t`Collapse Components`}
              expandLabel={t`Expand Components`}
              routePath="/components"
              isOnPage={isComponentsPage}
              activeHash={componentsHash}
              sections={componentsSections}
            />
            <CollapsibleMenu
              groupKey="examples"
              icon={LayersIcon}
              label={<Trans>Examples</Trans>}
              collapseLabel={t`Collapse Examples`}
              expandLabel={t`Expand Examples`}
              routePath="/components/examples"
              isOnPage={isExamplesPage}
              activeHash={examplesHash}
              sections={examplesSections}
            />
          </SidebarMenuCollapsibleProvider>

          <SidebarMenuItem>
            <SidebarMenuButton asChild={true} isActive={isChartsPage} tooltip={t`Charts`}>
              <RouterLink to="/components/charts">
                <ChartsIcon />
                <span>{chartsLabel}</span>
              </RouterLink>
            </SidebarMenuButton>
          </SidebarMenuItem>
          <SidebarMenuItem>
            <SidebarMenuButton asChild={true} isActive={isEmailsPage} tooltip={t`Emails`}>
              <RouterLink to="/components/emails">
                <EmailsIcon />
                <span>{emailsLabel}</span>
              </RouterLink>
            </SidebarMenuButton>
          </SidebarMenuItem>
        </SidebarMenu>
      </SidebarGroupContent>
    </SidebarGroup>
  );

  return (
    <Sidebar collapsible="icon" mobileContent={<PreviewMobileMenu>{renderNavigationGroup()}</PreviewMobileMenu>}>
      <SidebarHeader>
        <PreviewAvatarMenu />
      </SidebarHeader>
      <SidebarContent>{renderNavigationGroup()}</SidebarContent>
      <SidebarRail />
    </Sidebar>
  );
}
