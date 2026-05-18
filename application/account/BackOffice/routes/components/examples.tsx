import { t } from "@lingui/core/macro";
import { AppLayout } from "@repo/ui/components/AppLayout";
import { SidebarInset, SidebarProvider } from "@repo/ui/components/Sidebar";
import { createFileRoute } from "@tanstack/react-router";
import { useCallback, useEffect, useMemo, useState } from "react";

import type { SampleDish } from "./-components/sampleDishData";

import { ComponentsSideMenu } from "./-components/ComponentsSideMenu";
import { DishDetailsSidePane } from "./-components/DishDetailsSidePane";
import { DishMultiSelectSidePane } from "./-components/DishMultiSelectSidePane";
import { ExamplesPreview } from "./-components/ExamplesPreview";
import { PreviewHeader } from "./-components/PreviewHeader";
import { examplesSections, findSectionLabel } from "./-components/previewSections";
import { sampleDishes } from "./-components/sampleDishData";

export const Route = createFileRoute("/components/examples")({
  staticData: { trackingTitle: "Examples" },
  component: ExamplesPage
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

function ExamplesPage() {
  const [selectedDish, setSelectedDish] = useState<SampleDish | null>(null);
  const [selectedDishes, setSelectedDishes] = useState<SampleDish[]>([]);
  const [summaryPaneEnabled, setSummaryPaneEnabled] = useState(false);

  const activeHash = useActiveHash("dialogs");
  const activeLabel = findSectionLabel(examplesSections, activeHash);
  const tabLabels = useMemo(
    () => Object.fromEntries(examplesSections.map((section) => [section.hash, section.label])),
    []
  );

  // "In play" = selected (the batch) plus the currently-highlighted/activated row. When two or more
  // distinct rows are in play, the details pane no longer fits -- we switch to the batch summary so
  // the user always sees the set, not a single row's details.
  const inPlayKeys = new Set<number>(selectedDishes.map((d) => d.id));
  if (selectedDish != null) {
    inPlayKeys.add(selectedDish.id);
  }
  const inPlayCount = inPlayKeys.size;
  const showSummaryPane = inPlayCount >= 2 && summaryPaneEnabled;
  // Details pane is only driven by explicit activation (click / Enter / Space). Esc clears
  // selectedDish so the pane stays closed even when the underlying row selection is still 1 row --
  // keyboard navigation and plain Cmd-click should not re-open the pane on their own.
  const showDetailsPane = selectedDish != null && inPlayCount === 1;

  const handleCloseDish = useCallback(() => {
    setSelectedDish(null);
  }, []);

  const getSidePane = () => {
    if (showSummaryPane) {
      return <DishMultiSelectSidePane dishes={selectedDishes} totalCount={sampleDishes.length} isOpen={true} />;
    }
    if (showDetailsPane && selectedDish) {
      return <DishDetailsSidePane dish={selectedDish} isOpen={true} onClose={handleCloseDish} />;
    }
    return undefined;
  };

  return (
    <SidebarProvider>
      <ComponentsSideMenu />
      <SidebarInset>
        <AppLayout
          variant="full"
          browserTitle={t`Examples`}
          title={activeLabel}
          beforeHeader={<PreviewHeader currentPage="examples" defaultTab="dialogs" tabLabels={tabLabels} />}
          sidePane={getSidePane()}
        >
          <ExamplesPreview
            selectedDish={selectedDish}
            onDishSelect={setSelectedDish}
            onSelectedDishesChange={setSelectedDishes}
            onSummaryPaneChange={setSummaryPaneEnabled}
          />
        </AppLayout>
      </SidebarInset>
    </SidebarProvider>
  );
}
