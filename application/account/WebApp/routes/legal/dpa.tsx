import { MarkdownRenderer } from "@repo/ui/components/MarkdownRenderer";
import { createFileRoute } from "@tanstack/react-router";
import PublicFooter from "@/federated-modules/public/PublicFooter";
import PublicNavigation from "@/federated-modules/public/PublicNavigation";

export const Route = createFileRoute("/legal/dpa")({
  component: DataProcessingAgreement
});

function DataProcessingAgreement() {
  return (
    <main className="flex min-h-screen w-full flex-col">
      <div className="flex flex-1 flex-col bg-background">
        <PublicNavigation />
        <div className="mx-auto w-full max-w-4xl flex-1 px-6 py-12">
          <MarkdownRenderer path="/legal/documents/dpa.md" />
        </div>
      </div>
      <PublicFooter />
    </main>
  );
}
