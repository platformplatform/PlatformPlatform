import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Button } from "@repo/ui/components/Button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger
} from "@repo/ui/components/DropdownMenu";
import { ChevronDownIcon, SaveIcon } from "lucide-react";

export function SplitButtons() {
  return (
    <div className="flex flex-wrap items-center gap-3">
      <div className="inline-flex">
        <Button className="rounded-r-none">
          <SaveIcon />
          <Trans>Save</Trans>
        </Button>
        <DropdownMenu>
          <DropdownMenuTrigger
            render={
              <Button
                size="icon"
                aria-label={t`More save options`}
                className="rounded-l-none border-l border-primary-foreground/20"
              >
                <ChevronDownIcon />
              </Button>
            }
          />
          <DropdownMenuContent align="end">
            <DropdownMenuItem>
              <Trans>Save</Trans>
            </DropdownMenuItem>
            <DropdownMenuItem>
              <Trans>Save as...</Trans>
            </DropdownMenuItem>
            <DropdownMenuItem>
              <Trans>Save and close</Trans>
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      </div>
      <div className="inline-flex">
        <Button variant="secondary" className="rounded-r-none">
          <Trans>Export</Trans>
        </Button>
        <DropdownMenu>
          <DropdownMenuTrigger
            render={
              <Button
                variant="secondary"
                size="icon"
                aria-label={t`More export options`}
                className="rounded-l-none border-l border-border"
              >
                <ChevronDownIcon />
              </Button>
            }
          />
          <DropdownMenuContent align="end">
            <DropdownMenuItem>
              <Trans>Export as CSV</Trans>
            </DropdownMenuItem>
            <DropdownMenuItem>
              <Trans>Export as JSON</Trans>
            </DropdownMenuItem>
            <DropdownMenuItem>
              <Trans>Export as PDF</Trans>
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      </div>
      <div className="inline-flex">
        <Button variant="outline" className="rounded-r-none">
          <Trans>Merge</Trans>
        </Button>
        <DropdownMenu>
          <DropdownMenuTrigger
            render={
              <Button
                variant="outline"
                size="icon"
                aria-label={t`More merge options`}
                className="rounded-l-none border-l-0"
              >
                <ChevronDownIcon />
              </Button>
            }
          />
          <DropdownMenuContent align="end">
            <DropdownMenuItem>
              <Trans>Merge commit</Trans>
            </DropdownMenuItem>
            <DropdownMenuItem>
              <Trans>Squash and merge</Trans>
            </DropdownMenuItem>
            <DropdownMenuItem>
              <Trans>Rebase and merge</Trans>
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      </div>
    </div>
  );
}
