import { t } from "@lingui/core/macro";
import { AppLayout } from "@repo/ui/components/AppLayout";
import { SidebarInset, SidebarProvider } from "@repo/ui/components/Sidebar";
import { createFileRoute } from "@tanstack/react-router";

import { BackOfficeSideMenu } from "@/shared/components/BackOfficeSideMenu";

import { DashboardSections } from "./-components/DashboardSections";

export const Route = createFileRoute("/")({
  staticData: { trackingTitle: "Back Office dashboard" },
  component: DashboardPage
});

function DashboardPage() {
  return (
    <SidebarProvider>
      <BackOfficeSideMenu />
      <SidebarInset>
        <AppLayout variant="center" maxWidth="80rem" browserTitle={t`Dashboard`}>
          <DashboardSections />
        </AppLayout>
      </SidebarInset>
    </SidebarProvider>
  );
}
