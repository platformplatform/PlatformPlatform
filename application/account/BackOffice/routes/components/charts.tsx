import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { AppLayout } from "@repo/ui/components/AppLayout";
import { SidebarInset, SidebarProvider } from "@repo/ui/components/Sidebar";
import { createFileRoute } from "@tanstack/react-router";

import { ChartsPreview } from "./-components/ChartsPreview";
import { ComponentsSideMenu } from "./-components/ComponentsSideMenu";
import { PreviewHeader } from "./-components/PreviewHeader";

export const Route = createFileRoute("/components/charts")({
  staticData: { trackingTitle: "Charts" },
  component: ChartsPage
});

function ChartsPage() {
  return (
    <SidebarProvider>
      <ComponentsSideMenu />
      <SidebarInset>
        <AppLayout
          variant="full"
          browserTitle={t`Charts`}
          title={<Trans>Charts</Trans>}
          beforeHeader={<PreviewHeader currentPage="charts" defaultTab="" tabLabels={{ "": <Trans>Charts</Trans> }} />}
        >
          <ChartsPreview />
        </AppLayout>
      </SidebarInset>
    </SidebarProvider>
  );
}
