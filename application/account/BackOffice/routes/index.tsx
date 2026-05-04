import { t } from "@lingui/core/macro";
import { AppLayout } from "@repo/ui/components/AppLayout";
import { SidebarInset, SidebarProvider } from "@repo/ui/components/Sidebar";
import { createFileRoute } from "@tanstack/react-router";

import { BackOfficeSideMenu } from "@/shared/components/BackOfficeSideMenu";

import { DashboardSections } from "./-components/DashboardSections";

export const Route = createFileRoute("/")({
  staticData: { trackingTitle: "Back office dashboard" },
  component: DashboardPage
});

function DashboardPage() {
  return (
    <SidebarProvider>
      <BackOfficeSideMenu />
      <SidebarInset>
        <AppLayout
          variant="center"
          maxWidth="64rem"
          browserTitle={t`Dashboard`}
          title={t`Welcome to the Back Office`}
          subtitle={t`Manage accounts, view system data, see exceptions, and perform various tasks for operational and support teams.`}
        >
          <DashboardSections />
        </AppLayout>
      </SidebarInset>
    </SidebarProvider>
  );
}
