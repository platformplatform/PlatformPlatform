import { t } from "@lingui/core/macro";
import { AppLayout } from "@repo/ui/components/AppLayout";
import { createFileRoute } from "@tanstack/react-router";

export const Route = createFileRoute("/back-office/")({
  staticData: { trackingTitle: "Back office" },
  component: Home
});

export default function Home() {
  return (
    <AppLayout
      browserTitle={t`Dashboard`}
      title={t`Welcome to the Back Office`}
      subtitle={t`Manage tenants, view system data, see exceptions, and perform various tasks for operational and support teams.`}
    >
      <div />
    </AppLayout>
  );
}
