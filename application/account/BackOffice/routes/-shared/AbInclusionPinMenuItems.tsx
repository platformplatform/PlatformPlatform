import { Trans } from "@lingui/react/macro";
import { DropdownMenuItem, DropdownMenuSeparator } from "@repo/ui/components/DropdownMenu";
import { CircleDotIcon } from "lucide-react";

// Single menu item that opens the A/B inclusion pin dialog. The dialog itself MUST be rendered as
// a sibling of the DropdownMenu — keeping it inside the dropdown's subtree causes BaseUI to unmount
// the dialog the instant the menu closes (the dialog flashes for ~500ms then disappears).
interface AbInclusionPinMenuItemsProps {
  withLeadingSeparator?: boolean;
  onSelect: () => void;
}

export function AbInclusionPinMenuItems({
  withLeadingSeparator = false,
  onSelect
}: Readonly<AbInclusionPinMenuItemsProps>) {
  return (
    <>
      {withLeadingSeparator && <DropdownMenuSeparator />}
      <DropdownMenuItem trackingLabel="Open feature flag rollouts dialog" onClick={onSelect}>
        <CircleDotIcon className="size-4" />
        <Trans>Feature flag rollouts</Trans>
      </DropdownMenuItem>
    </>
  );
}
