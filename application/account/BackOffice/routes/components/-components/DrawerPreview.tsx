import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Button } from "@repo/ui/components/Button";
import {
  Drawer,
  DrawerClose,
  DrawerContent,
  DrawerDescription,
  DrawerFooter,
  DrawerHeader,
  DrawerTitle,
  DrawerTrigger
} from "@repo/ui/components/Drawer";
import { TextAreaField } from "@repo/ui/components/TextAreaField";
import { cn } from "@repo/ui/utils";

type Direction = "bottom" | "top" | "left" | "right";

function NoteDrawer({
  direction,
  label,
  contentClassName
}: {
  direction: Direction;
  label: React.ReactNode;
  contentClassName?: string;
}) {
  const isHorizontal = direction === "left" || direction === "right";
  return (
    <Drawer direction={direction}>
      <DrawerTrigger asChild>
        <Button variant="outline">{label}</Button>
      </DrawerTrigger>
      <DrawerContent className={contentClassName}>
        <div className={isHorizontal ? "flex h-full flex-col" : "mx-auto w-full max-w-[64rem]"}>
          <DrawerHeader>
            <DrawerTitle>
              <Trans>Quick note</Trans>
            </DrawerTitle>
            <DrawerDescription>
              <Trans>Jot down a thought. It will be attached to the current recipe.</Trans>
            </DrawerDescription>
          </DrawerHeader>
          <div className="px-4">
            <TextAreaField
              label={t`Note`}
              placeholder={t`E.g. doubled the garlic, used a cast iron pan...`}
              lines={4}
            />
          </div>
          <DrawerFooter className="flex-row justify-end gap-2">
            <DrawerClose asChild>
              <Button variant="ghost">
                <Trans>Cancel</Trans>
              </Button>
            </DrawerClose>
            <Button>
              <Trans>Save note</Trans>
            </Button>
          </DrawerFooter>
        </div>
      </DrawerContent>
    </Drawer>
  );
}

export function DrawerPreview() {
  return (
    <section className="flex flex-col gap-3">
      <h3>
        <Trans>Drawer</Trans>
      </h3>
      <p className="text-sm text-muted-foreground">
        <Trans>
          Mobile-first sheet with a drag handle and swipe-to-dismiss. Reach for it when content is short and the user
          should be able to flick it away. Left and right drawers are narrow by default; top and bottom span the full
          width unless you constrain DrawerContent with mx-auto and max-w-*.
        </Trans>
      </p>
      <div className="flex flex-wrap gap-2">
        <NoteDrawer direction="bottom" label={<Trans>Bottom drawer</Trans>} />
        <NoteDrawer direction="top" label={<Trans>Top drawer</Trans>} />
        <NoteDrawer direction="left" label={<Trans>Left drawer</Trans>} />
        <NoteDrawer direction="right" label={<Trans>Right drawer</Trans>} />
        <NoteDrawer
          direction="bottom"
          label={<Trans>Narrow bottom drawer</Trans>}
          contentClassName={cn("mx-auto max-w-md")}
        />
      </div>
    </section>
  );
}
