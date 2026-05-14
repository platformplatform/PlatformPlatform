import { t } from "@lingui/core/macro";
import { Button } from "@repo/ui/components/Button";
import { DropdownMenu, DropdownMenuContent, DropdownMenuTrigger } from "@repo/ui/components/DropdownMenu";
import { Tooltip, TooltipContent, TooltipTrigger } from "@repo/ui/components/Tooltip";
import { MoreVerticalIcon } from "lucide-react";
import { useState } from "react";

import type { AbInclusionPin } from "@/shared/lib/api/client";

import { useMe } from "@/shared/hooks/useMe";

import { AbInclusionPinMenuItems } from "../../-shared/AbInclusionPinMenuItems";
import { SetAbInclusionPinDialog } from "../../-shared/SetAbInclusionPinDialog";

interface UserActionsMenuProps {
  userId: string;
  userLabel: string;
  abInclusionPin: AbInclusionPin | null | undefined;
}

export function UserActionsMenu({ userId, userLabel, abInclusionPin }: Readonly<UserActionsMenuProps>) {
  const { data: me } = useMe();
  const [isPinDialogOpen, setIsPinDialogOpen] = useState(false);

  if (!me?.isAdmin) {
    return null;
  }

  return (
    <>
      <DropdownMenu trackingTitle="User actions">
        <Tooltip>
          <TooltipTrigger
            render={
              <DropdownMenuTrigger
                render={
                  <Button variant="outline" size="icon-sm" aria-label={t`User actions`}>
                    <MoreVerticalIcon className="size-4" />
                  </Button>
                }
              />
            }
          />
          <TooltipContent>{t`User actions`}</TooltipContent>
        </Tooltip>
        <DropdownMenuContent align="end">
          <AbInclusionPinMenuItems onSelect={() => setIsPinDialogOpen(true)} />
        </DropdownMenuContent>
      </DropdownMenu>

      <SetAbInclusionPinDialog
        entity="user"
        entityId={userId}
        entityLabel={userLabel}
        currentPin={abInclusionPin ?? null}
        isOpen={isPinDialogOpen}
        onOpenChange={setIsPinDialogOpen}
      />
    </>
  );
}
