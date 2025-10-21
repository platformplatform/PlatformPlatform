import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { useUserInfo } from "@repo/infrastructure/auth/hooks";
import { Button } from "@repo/ui/components/Button";
import { PlusIcon } from "lucide-react";
import { useState } from "react";
import { CreateTeamDialog } from "./CreateTeamDialog";

export function TeamsToolbar() {
  const userInfo = useUserInfo();
  const [isCreateDialogOpen, setIsCreateDialogOpen] = useState(false);

  const isOwner = userInfo?.role === "Owner";

  if (!isOwner) {
    return null;
  }

  return (
    <>
      <div className="-mt-5 mb-4 flex items-center justify-end gap-2 bg-background/95 pt-5 backdrop-blur-sm">
        <div className="mt-6 flex items-center gap-2">
          <Button variant="primary" onPress={() => setIsCreateDialogOpen(true)} aria-label={t`Create Team`}>
            <PlusIcon className="h-5 w-5" />
            <span className="hidden sm:inline">
              <Trans>Create Team</Trans>
            </span>
          </Button>
        </div>
      </div>

      <CreateTeamDialog isOpen={isCreateDialogOpen} onOpenChange={setIsCreateDialogOpen} />
    </>
  );
}
