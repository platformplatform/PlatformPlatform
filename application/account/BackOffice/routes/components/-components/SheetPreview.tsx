import { Trans } from "@lingui/react/macro";
import { Button } from "@repo/ui/components/Button";
import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetFooter,
  SheetHeader,
  SheetTitle,
  SheetTrigger
} from "@repo/ui/components/Sheet";

type Side = "right" | "left" | "top" | "bottom";

function FilterSheet({
  side,
  label,
  contentClassName
}: {
  side: Side;
  label: React.ReactNode;
  contentClassName?: string;
}) {
  return (
    <Sheet>
      <SheetTrigger render={<Button variant="outline">{label}</Button>} />
      <SheetContent side={side} className={contentClassName}>
        <SheetHeader>
          <SheetTitle>
            <Trans>Filters</Trans>
          </SheetTitle>
          <SheetDescription>
            <Trans>Refine the recipe list. Changes apply when you close the sheet.</Trans>
          </SheetDescription>
        </SheetHeader>
        <div className="flex-1 overflow-y-auto px-4 text-muted-foreground">
          <p>
            <Trans>Drop your filter controls here. The sheet handles scrolling and dismissal.</Trans>
          </p>
        </div>
        <SheetFooter>
          <Button>
            <Trans>Apply filters</Trans>
          </Button>
        </SheetFooter>
      </SheetContent>
    </Sheet>
  );
}

export function SheetPreview() {
  return (
    <section className="flex flex-col gap-3">
      <h3>
        <Trans>Sheet</Trans>
      </h3>
      <p className="text-sm text-muted-foreground">
        <Trans>
          Side-anchored panel built on the same Dialog primitive. Use it for desktop side surfaces like filter sidebars,
          secondary navigation, or detail editors. Left and right sheets are narrow by default; top and bottom span the
          full width unless you constrain SheetContent with mx-auto and max-w-*. Compare with SidePane (Custom) which is
          the docked-by-default app-shell variant.
        </Trans>
      </p>
      <div className="flex flex-wrap gap-2">
        <FilterSheet side="right" label={<Trans>Right sheet</Trans>} />
        <FilterSheet side="left" label={<Trans>Left sheet</Trans>} />
        <FilterSheet side="top" label={<Trans>Top sheet</Trans>} />
        <FilterSheet side="bottom" label={<Trans>Bottom sheet</Trans>} />
        <FilterSheet side="bottom" label={<Trans>Narrow bottom sheet</Trans>} contentClassName="mx-auto max-w-md" />
      </div>
    </section>
  );
}
