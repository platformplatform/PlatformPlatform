import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { AppLayout } from "@repo/ui/components/AppLayout";
import { Breadcrumb, BreadcrumbItem, BreadcrumbList, BreadcrumbPage } from "@repo/ui/components/Breadcrumb";
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
      beforeHeader={
        <Breadcrumb>
          <BreadcrumbList>
            <BreadcrumbItem>
              <BreadcrumbPage>
                <Trans>Back office</Trans>
              </BreadcrumbPage>
            </BreadcrumbItem>
          </BreadcrumbList>
        </Breadcrumb>
      }
    >
      <div />
    </AppLayout>
  );
}
