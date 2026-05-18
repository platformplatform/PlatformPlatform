import { t } from "@lingui/core/macro";
import { AppLayout } from "@repo/ui/components/AppLayout";
import { SidebarInset, SidebarProvider } from "@repo/ui/components/Sidebar";
import { createFileRoute } from "@tanstack/react-router";
import { useEffect, useMemo, useState } from "react";

import { ComponentPreview } from "./-components/ComponentPreview";
import { ComponentsSideMenu } from "./-components/ComponentsSideMenu";
import { PreviewHeader } from "./-components/PreviewHeader";
import { componentsSections, findSectionLabel } from "./-components/previewSections";

export const Route = createFileRoute("/components/")({
  staticData: { trackingTitle: "Components" },
  component: ComponentsPage
});

function useActiveHash(defaultHash: string) {
  const [hash, setHash] = useState(() => window.location.hash.replace("#", "") || defaultHash);
  useEffect(() => {
    const handle = () => setHash(window.location.hash.replace("#", "") || defaultHash);
    window.addEventListener("hashchange", handle);
    return () => window.removeEventListener("hashchange", handle);
  }, [defaultHash]);
  return hash;
}

function ComponentsPage() {
  const activeHash = useActiveHash("controls");
  const activeLabel = findSectionLabel(componentsSections, activeHash);
  const tabLabels = useMemo(
    () => Object.fromEntries(componentsSections.map((section) => [section.hash, section.label])),
    []
  );

  return (
    <SidebarProvider>
      <ComponentsSideMenu />
      <SidebarInset>
        <AppLayout
          variant="full"
          browserTitle={t`Components`}
          title={activeLabel}
          beforeHeader={<PreviewHeader currentPage="components" defaultTab="controls" tabLabels={tabLabels} />}
        >
          <ComponentPreview />
        </AppLayout>
      </SidebarInset>
    </SidebarProvider>
  );
}
