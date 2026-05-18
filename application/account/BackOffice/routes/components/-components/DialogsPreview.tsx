import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Button } from "@repo/ui/components/Button";
import { SwitchField } from "@repo/ui/components/SwitchField";
import { ToggleGroup, ToggleGroupItem } from "@repo/ui/components/ToggleGroup";
import { ChefHatIcon, Share2Icon } from "lucide-react";
import { useState } from "react";

import { AlertDialogsPreview } from "./AlertDialogsPreview";
import { type DialogSize } from "./dialogSize";
import { RecipeEditorDialog } from "./RecipeEditorDialog";
import { ShareRecipeDialog } from "./ShareRecipeDialog";

export function DialogsPreview() {
  const [isRecipeOpen, setIsRecipeOpen] = useState(false);
  const [isShareOpen, setIsShareOpen] = useState(false);
  const [dirtyDialog, setDirtyDialog] = useState(true);
  const [showToasts, setShowToasts] = useState(true);
  const [simulateErrors, setSimulateErrors] = useState(false);
  const [dialogSize, setDialogSize] = useState<DialogSize>("md");

  const options = { dirtyDialog, showToasts, simulateErrors, size: dialogSize };

  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-col gap-2">
        <h4>
          <Trans>Dialogs</Trans>
        </h4>
        <div className="flex flex-wrap items-center gap-x-6 gap-y-2">
          <SwitchField
            label={t`Dirty dialog`}
            name="dirty-dialog"
            checked={dirtyDialog}
            onCheckedChange={(v) => setDirtyDialog(!!v)}
          />
          <SwitchField
            label={t`Show toast`}
            name="show-toasts"
            checked={showToasts}
            onCheckedChange={(v) => setShowToasts(!!v)}
          />
          <SwitchField
            label={t`Simulate errors`}
            name="simulate-errors"
            checked={simulateErrors}
            onCheckedChange={(v) => setSimulateErrors(!!v)}
          />
          <ToggleGroup
            variant="outline"
            value={[dialogSize]}
            onValueChange={(values) => {
              if (values.length > 0) {
                setDialogSize(values[0] as DialogSize);
              }
            }}
          >
            <ToggleGroupItem value="sm">
              <Trans>Small</Trans>
            </ToggleGroupItem>
            <ToggleGroupItem value="md">
              <Trans>Medium</Trans>
            </ToggleGroupItem>
            <ToggleGroupItem value="lg">
              <Trans>Large</Trans>
            </ToggleGroupItem>
            <ToggleGroupItem value="xl">
              <Trans>Extra large</Trans>
            </ToggleGroupItem>
            <ToggleGroupItem value="2xl">
              <Trans>2X large</Trans>
            </ToggleGroupItem>
          </ToggleGroup>
        </div>
        <div className="flex flex-wrap items-center gap-3">
          <Button variant="outline" onClick={() => setIsRecipeOpen(true)}>
            <ChefHatIcon />
            <Trans>Edit recipe</Trans>
          </Button>
          <Button variant="outline" onClick={() => setIsShareOpen(true)}>
            <Share2Icon />
            <Trans>Share recipe</Trans>
          </Button>
        </div>
        <RecipeEditorDialog isOpen={isRecipeOpen} onOpenChange={setIsRecipeOpen} {...options} />
        <ShareRecipeDialog isOpen={isShareOpen} onOpenChange={setIsShareOpen} {...options} />
      </div>

      <div className="flex flex-col gap-2">
        <h4>
          <Trans>Alert dialogs</Trans>
        </h4>
        <AlertDialogsPreview showToasts={showToasts} />
      </div>
    </div>
  );
}
