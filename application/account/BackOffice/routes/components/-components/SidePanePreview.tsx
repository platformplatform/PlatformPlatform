import { Trans } from "@lingui/react/macro";
import { Button } from "@repo/ui/components/Button";
import { SidePane, SidePaneBody, SidePaneClose, SidePaneFooter, SidePaneHeader } from "@repo/ui/components/SidePane";
import { ChefHatIcon } from "lucide-react";
import { useState } from "react";

export function SidePanePreview() {
  const [isSidePaneOpen, setIsSidePaneOpen] = useState(false);

  return (
    <div className="flex flex-col gap-2">
      <h4>
        <Trans>Side pane</Trans>
      </h4>
      <Button variant="outline" onClick={() => setIsSidePaneOpen(true)}>
        <ChefHatIcon />
        <Trans>Open side pane</Trans>
      </Button>

      <SidePane isOpen={isSidePaneOpen} onOpenChange={setIsSidePaneOpen} trackingTitle="Component preview">
        <SidePaneHeader>
          <Trans>Recipe details</Trans>
        </SidePaneHeader>
        <SidePaneBody>
          <div className="space-y-4">
            <div>
              <h5 className="mb-2 font-medium">
                <Trans>Description</Trans>
              </h5>
              <p className="text-sm text-muted-foreground">
                <Trans>
                  This is a side pane component that slides in from the right. It can adapt to fullscreen mode on
                  smaller screens.
                </Trans>
              </p>
            </div>
            <div>
              <h5 className="mb-2 font-medium">
                <Trans>Features</Trans>
              </h5>
              <ul className="list-inside list-disc space-y-1 text-sm text-muted-foreground">
                <li>
                  <Trans>Responsive layout</Trans>
                </li>
                <li>
                  <Trans>Fullscreen on mobile</Trans>
                </li>
                <li>
                  <Trans>Keyboard accessible</Trans>
                </li>
                <li>
                  <Trans>Escape to close</Trans>
                </li>
              </ul>
            </div>
          </div>
        </SidePaneBody>
        <SidePaneFooter className="flex gap-3">
          <Button variant="secondary" className="flex-1">
            <Trans>Cancel</Trans>
          </Button>
          <SidePaneClose render={<Button className="flex-1" />}>
            <Trans>Done</Trans>
          </SidePaneClose>
        </SidePaneFooter>
      </SidePane>
    </div>
  );
}
