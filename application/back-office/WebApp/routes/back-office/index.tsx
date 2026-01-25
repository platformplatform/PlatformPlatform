import { t } from "@lingui/core/macro";
import { createFileRoute } from "@tanstack/react-router";
import { AppLayout } from "@/shared/components/AppLayout";

export const Route = createFileRoute("/back-office/")({
  component: Home
});

export default function Home() {
  return (
    <AppLayout
      title={t`Welcome to the Back Office`}
      subtitle={t`Manage tenants, view system data, see exceptions, and perform various tasks for operational and support teams.`}
    >
      <div />
    </AppLayout>
  );
}
