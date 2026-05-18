import { useEffect, useState } from "react";

import type { SampleDish } from "./sampleDishData";

import { CardsPreview } from "./CardsPreview";
import { DateFormatPreview } from "./DateFormatPreview";
import { DialogsPreview } from "./DialogsPreview";
import { EmptyPreview } from "./EmptyPreview";
import { SkeletonPreview } from "./SkeletonPreview";
import { TablePreview } from "./TablePreview";

interface ExamplesPreviewProps {
  selectedDish?: SampleDish | null;
  onDishSelect?: (dish: SampleDish | null) => void;
  onSelectedDishesChange?: (dishes: SampleDish[]) => void;
  onSummaryPaneChange?: (enabled: boolean) => void;
}

const DEFAULT_SECTION = "dialogs";

export function ExamplesPreview({
  selectedDish,
  onDishSelect,
  onSelectedDishesChange,
  onSummaryPaneChange
}: ExamplesPreviewProps) {
  const [activeSection, setActiveSection] = useState(() => window.location.hash.replace("#", "") || DEFAULT_SECTION);

  useEffect(() => {
    const handleHashChange = () => {
      const next = window.location.hash.replace("#", "") || DEFAULT_SECTION;
      // Leaving the tables view clears the selected row so the details pane doesn't remain open.
      setActiveSection((prev) => {
        if (prev === "tables" && next !== "tables") {
          onDishSelect?.(null);
        }
        return next;
      });
    };
    window.addEventListener("hashchange", handleHashChange);
    return () => window.removeEventListener("hashchange", handleHashChange);
  }, [onDishSelect]);

  return (
    <div className="flex flex-1 flex-col">
      {activeSection === "dialogs" && <DialogsPreview />}
      {activeSection === "cards" && (
        <div className="flex flex-col gap-6">
          <CardsPreview />
          <DateFormatPreview />
        </div>
      )}
      {activeSection === "tables" && (
        <TablePreview
          selectedDish={selectedDish}
          onDishSelect={onDishSelect}
          onSelectedDishesChange={onSelectedDishesChange}
          onSummaryPaneChange={onSummaryPaneChange}
        />
      )}
      {activeSection === "empty" && <EmptyPreview />}
      {activeSection === "skeleton" && <SkeletonPreview />}
    </div>
  );
}
