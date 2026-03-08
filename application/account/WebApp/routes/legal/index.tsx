import { createFileRoute } from "@tanstack/react-router";

import PublicFooter from "@/federated-modules/public/PublicFooter";
import PublicNavigation from "@/federated-modules/public/PublicNavigation";

import { AzureInfrastructureSection } from "./-components/AzureInfrastructureSection";
import { ComplianceResourcesSection } from "./-components/ComplianceResourcesSection";
import { LegalDocumentsSection } from "./-components/LegalDocumentsSection";

export const Route = createFileRoute("/legal/")({
  staticData: { trackingTitle: "Legal and compliance" },
  component: LegalIndex
});

function LegalIndex() {
  return (
    <main className="flex min-h-screen w-full flex-col">
      <div className="flex flex-1 flex-col">
        <PublicNavigation />
        <LegalDocumentsSection />
        <AzureInfrastructureSection />
        <ComplianceResourcesSection />
      </div>
      <PublicFooter />
    </main>
  );
}
