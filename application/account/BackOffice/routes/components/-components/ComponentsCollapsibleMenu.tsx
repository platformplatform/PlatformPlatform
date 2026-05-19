import type { LucideIcon } from "lucide-react";

import {
  SidebarMenuAction,
  SidebarMenuButton,
  SidebarMenuFlyout,
  SidebarMenuItem,
  SidebarMenuSub,
  SidebarMenuSubButton,
  SidebarMenuSubItem,
  useSidebarMenuCollapsible
} from "@repo/ui/components/Sidebar";
import { Link as RouterLink } from "@tanstack/react-router";
import { ChevronRightIcon } from "lucide-react";
import { useEffect, useState } from "react";

import type { PreviewSection } from "./previewSections";

export function useHash(defaultHash: string) {
  const [hash, setHash] = useState(() => window.location.hash.replace("#", "") || defaultHash);
  useEffect(() => {
    const handle = () => setHash(window.location.hash.replace("#", "") || defaultHash);
    window.addEventListener("hashchange", handle);
    return () => window.removeEventListener("hashchange", handle);
  }, [defaultHash]);
  return hash;
}

type CollapsibleMenuProps = Readonly<{
  groupKey: string;
  icon: LucideIcon;
  label: React.ReactNode;
  collapseLabel: string;
  expandLabel: string;
  routePath: string;
  isOnPage: boolean;
  activeHash: string;
  sections: readonly PreviewSection[];
}>;

export function CollapsibleMenu({
  groupKey,
  icon: Icon,
  label,
  collapseLabel,
  expandLabel,
  routePath,
  isOnPage,
  activeHash,
  sections
}: CollapsibleMenuProps) {
  // Arriving at a matching page auto-expands this group (and auto-collapses any sibling).
  const { isExpanded, toggle, expand } = useSidebarMenuCollapsible(groupKey);
  useEffect(() => {
    if (isOnPage) {
      expand();
    }
  }, [isOnPage, expand]);

  // In collapsed sidebar mode, hovering the top-level icon shows a popover listing the sub sections —
  // a compact version of the expanded sub list. Built from the same `sections` data so there's no
  // duplication.
  const flyout = (
    <div className="flex flex-col gap-1 p-1">
      <div className="px-2 pt-1 pb-0.5 text-xs font-medium text-muted-foreground uppercase">{label}</div>
      {sections.map(({ hash, label: sectionLabel, icon: SectionIcon }) => (
        <RouterLink
          key={hash}
          to={routePath}
          hash={hash}
          data-active={isOnPage && activeHash === hash}
          className="flex items-center gap-3 rounded-md px-2 py-1.5 text-sm text-muted-foreground outline-ring hover:bg-sidebar-accent hover:text-sidebar-accent-foreground focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 data-[active=true]:font-medium data-[active=true]:text-foreground [&>svg]:size-4 [&>svg]:shrink-0"
        >
          <SectionIcon />
          <span>{sectionLabel}</span>
        </RouterLink>
      ))}
    </div>
  );

  return (
    <SidebarMenuItem>
      {/* Clicking the top-level button both navigates and expands the sub group. The route-change
          effect only fires on `isOnPage` transitions, so an explicit `expand()` on click also covers
          the "already on this page, user manually collapsed, clicks again" case. */}
      {/* Skip the tooltip here — the flyout already shows the group's label in its header, so the
          collapsed-sidebar tooltip would just duplicate it. `disabled={isExpanded}` suppresses the
          flyout when the group's sub items are already visible inline (expanded icon band), so they
          aren't shown twice. */}
      <SidebarMenuFlyout content={flyout} disabled={isExpanded}>
        <SidebarMenuButton asChild={true} isActive={isOnPage} onClick={expand}>
          <RouterLink to={routePath}>
            <Icon />
            <span>{label}</span>
          </RouterLink>
        </SidebarMenuButton>
      </SidebarMenuFlyout>
      <SidebarMenuAction onClick={toggle} aria-label={isExpanded ? collapseLabel : expandLabel}>
        <ChevronRightIcon className={`transition-transform duration-100 ${isExpanded ? "rotate-90" : ""}`} />
      </SidebarMenuAction>
      <SidebarMenuSub isExpanded={isExpanded}>
        {sections.map(({ hash, label: sectionLabel, icon: SectionIcon }) => (
          <SidebarMenuSubItem key={hash}>
            <SidebarMenuSubButton
              asChild={true}
              isActive={isOnPage && activeHash === hash}
              tooltip={{ children: sectionLabel }}
            >
              {isOnPage ? (
                // Same-route hash change: native anchor so the browser updates window.location.hash
                // and fires `hashchange`, which the content components listen for.
                <a href={`#${hash}`}>
                  <SectionIcon />
                  <span>{sectionLabel}</span>
                </a>
              ) : (
                <RouterLink to={routePath} hash={hash}>
                  <SectionIcon />
                  <span>{sectionLabel}</span>
                </RouterLink>
              )}
            </SidebarMenuSubButton>
          </SidebarMenuSubItem>
        ))}
      </SidebarMenuSub>
    </SidebarMenuItem>
  );
}
