import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Empty, EmptyDescription, EmptyHeader, EmptyTitle } from "@repo/ui/components/Empty";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@repo/ui/components/Tabs";
import { ActivityIcon } from "lucide-react";

import { UserSectionHeader } from "./UserSectionHeader";

// Application Insights telemetry is delivered separately. Until that backend lands, the tabs render empty
// states so the User detail page can ship without a hard dependency on the telemetry pipeline.
export function UserTelemetrySection() {
  return (
    <section className="flex flex-col gap-3">
      <UserSectionHeader
        icon={ActivityIcon}
        title={<Trans>App Insights · Telemetry</Trans>}
        description={<Trans>Live data from Application Insights, scoped to this user</Trans>}
      />
      <Tabs defaultValue="exceptions" className="w-full">
        <TabsList>
          <TabsTrigger value="exceptions">
            <Trans>Exceptions</Trans>
          </TabsTrigger>
          <TabsTrigger value="page-views">
            <Trans>Page views</Trans>
          </TabsTrigger>
          <TabsTrigger value="custom-events">
            <Trans>Custom events</Trans>
          </TabsTrigger>
        </TabsList>
        <TabsContent value="exceptions">
          <TelemetryEmpty title={t`No exceptions`} description={t`No exceptions recorded for this user yet.`} />
        </TabsContent>
        <TabsContent value="page-views">
          <TelemetryEmpty title={t`No page views`} description={t`No page views recorded for this user yet.`} />
        </TabsContent>
        <TabsContent value="custom-events">
          <TelemetryEmpty title={t`No custom events`} description={t`No custom events recorded for this user yet.`} />
        </TabsContent>
      </Tabs>
    </section>
  );
}

function TelemetryEmpty({ title, description }: Readonly<{ title: string; description: string }>) {
  return (
    <Empty className="border bg-card">
      <EmptyHeader>
        <EmptyTitle>{title}</EmptyTitle>
        <EmptyDescription>{description}</EmptyDescription>
      </EmptyHeader>
    </Empty>
  );
}
