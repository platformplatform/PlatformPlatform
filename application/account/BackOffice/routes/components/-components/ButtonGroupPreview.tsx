import { Trans } from "@lingui/react/macro";
import { Button } from "@repo/ui/components/Button";
import { ButtonGroup } from "@repo/ui/components/ButtonGroup";
import { AlignCenterIcon, AlignLeftIcon, AlignRightIcon, ArrowDownIcon, ArrowUpIcon, MinusIcon } from "lucide-react";

export function ButtonGroupPreview() {
  return (
    <div className="flex flex-wrap items-center gap-3">
      <ButtonGroup>
        <Button variant="outline">
          <AlignLeftIcon />
          <Trans>Left</Trans>
        </Button>
        <Button variant="outline">
          <AlignCenterIcon />
          <Trans>Center</Trans>
        </Button>
        <Button variant="outline">
          <AlignRightIcon />
          <Trans>Right</Trans>
        </Button>
      </ButtonGroup>
      <ButtonGroup orientation="vertical" className="w-32">
        <Button variant="outline" className="w-full">
          <ArrowUpIcon />
          <Trans>Top</Trans>
        </Button>
        <Button variant="outline" className="w-full">
          <MinusIcon />
          <Trans>Middle</Trans>
        </Button>
        <Button variant="outline" className="w-full">
          <ArrowDownIcon />
          <Trans>Bottom</Trans>
        </Button>
      </ButtonGroup>
    </div>
  );
}
