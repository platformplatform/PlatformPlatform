import { Trans } from "@lingui/react/macro";
import { ResizableHandle, ResizablePanel, ResizablePanelGroup } from "@repo/ui/components/Resizable";

// Full-app-shell layout pattern using nested Resizable groups. Sized to the viewport so the whole
// layout is visible without scrolling. Wrapping in a sized <div> is required -- the library sets
// `height: 100%` inline on its own root, which overrides any Tailwind height class on the group.
// Sizes use rem strings where a fixed-pixel-like shape matters (sidemenu/sidepane widths and the
// top-bar column minimums); percentages are used where panels split a row evenly.
export function ResizablePreview() {
  return (
    <div className="flex flex-col gap-2">
      <h4>
        <Trans>Resizable app shell</Trans>
      </h4>
      <div className="h-[calc(100dvh-16rem)] min-h-100 overflow-hidden rounded-md border bg-card">
        <ResizablePanelGroup orientation="horizontal">
          {/* Left sidemenu: spans the full height (like a real app's sidemenu) */}
          <ResizablePanel defaultSize="14rem" minSize="4rem" maxSize="24rem">
            <PanelLabel title={<Trans>Left</Trans>} constraints="default 14rem · min 4rem · max 24rem" />
          </ResizablePanel>
          <ResizableHandle withHandle />

          {/* Everything right of the sidemenu: top bar + body stacked vertically */}
          <ResizablePanel>
            <div className="flex h-full flex-col">
              {/* Top bar: fixed height, three resizable columns */}
              <div className="h-12 shrink-0 border-b">
                <ResizablePanelGroup orientation="horizontal">
                  <ResizablePanel defaultSize={33} minSize="10rem">
                    <PanelLabel title={<Trans>Top left</Trans>} constraints="default 33% · min 10rem" />
                  </ResizablePanel>
                  <ResizableHandle />
                  <ResizablePanel defaultSize={34} minSize="10rem">
                    <PanelLabel title={<Trans>Top center</Trans>} constraints="default 34% · min 10rem" />
                  </ResizablePanel>
                  <ResizableHandle />
                  <ResizablePanel defaultSize={33} minSize="10rem">
                    <PanelLabel title={<Trans>Top right</Trans>} constraints="default 33% · min 10rem" />
                  </ResizablePanel>
                </ResizablePanelGroup>
              </div>

              {/* Body: main (with bottom dock) + right sidepane */}
              <div className="min-h-0 flex-1">
                <ResizablePanelGroup orientation="horizontal">
                  <ResizablePanel minSize="32rem">
                    <ResizablePanelGroup orientation="vertical">
                      <ResizablePanel minSize="16rem">
                        <PanelLabel
                          title={<Trans>Main</Trans>}
                          constraints="W: min 32rem | H: min 16rem (both fill remaining)"
                        />
                      </ResizablePanel>
                      <ResizableHandle withHandle />
                      <ResizablePanel defaultSize="16rem" minSize="6rem">
                        <PanelLabel
                          title={<Trans>Bottom</Trans>}
                          constraints="W: min 32rem | H: default 16rem · min 6rem"
                        />
                      </ResizablePanel>
                    </ResizablePanelGroup>
                  </ResizablePanel>
                  <ResizableHandle withHandle />
                  <ResizablePanel defaultSize="24rem" minSize="8rem" maxSize="40rem">
                    <PanelLabel title={<Trans>Right</Trans>} constraints="default 24rem · min 8rem · max 40rem" />
                  </ResizablePanel>
                </ResizablePanelGroup>
              </div>
            </div>
          </ResizablePanel>
        </ResizablePanelGroup>
      </div>
    </div>
  );
}

interface PanelLabelProps {
  title: React.ReactNode;
  constraints: string;
}

function PanelLabel({ title, constraints }: PanelLabelProps) {
  return (
    <div className="flex h-full flex-col items-center justify-center gap-1 p-2 text-center">
      <div>{title}</div>
      <div className="text-[0.7rem] text-muted-foreground">{constraints}</div>
    </div>
  );
}
