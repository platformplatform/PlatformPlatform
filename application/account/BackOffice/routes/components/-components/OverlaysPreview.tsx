import { AccordionPreview } from "./AccordionPreview";
import { DrawerPreview } from "./DrawerPreview";
import { HoverCardPreview } from "./HoverCardPreview";
import { SheetPreview } from "./SheetPreview";

export function OverlaysPreview() {
  return (
    <div className="flex flex-col gap-12">
      <AccordionPreview />
      <DrawerPreview />
      <SheetPreview />
      <HoverCardPreview />
    </div>
  );
}
