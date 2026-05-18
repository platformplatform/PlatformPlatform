import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Button } from "@repo/ui/components/Button";
import { DialogBody, DialogClose, DialogFooter, DialogForm } from "@repo/ui/components/Dialog";
import { TextField } from "@repo/ui/components/TextField";
import { ChevronRightIcon } from "lucide-react";

import { AvatarUpload } from "./AvatarUpload";

interface RecipeInfoStepProps {
  simulateErrors: boolean;
  onNext: () => void;
  onCancel: () => void;
  onChange: () => void;
}

export function RecipeInfoStep({ simulateErrors, onNext, onCancel, onChange }: Readonly<RecipeInfoStepProps>) {
  return (
    <DialogForm>
      <DialogBody>
        <div className="grid grid-cols-2 gap-4">
          <div className="col-span-2">
            <AvatarUpload onChange={onChange} />
          </div>
          <TextField
            autoFocus
            name="dishName"
            label={t`Dish name`}
            defaultValue="Spaghetti Carbonara"
            placeholder={t`E.g., Spaghetti Carbonara`}
            errorMessage={simulateErrors ? t`Dish name is required` : undefined}
            onChange={onChange}
          />
          <TextField
            name="chef"
            label={t`Chef`}
            defaultValue="Maria Rossi"
            placeholder={t`E.g., Maria Rossi`}
            onChange={onChange}
          />
          <TextField
            name="sourceUrl"
            label={t`Source`}
            type="email"
            defaultValue="recipes@trattoria.example"
            placeholder={t`recipes@example.com`}
            errorMessage={simulateErrors ? t`Please enter a valid email address` : undefined}
            onChange={onChange}
          />
          <TextField
            name="phone"
            label={t`Restaurant phone`}
            type="tel"
            defaultValue="+39 06 555 0123"
            placeholder={t`+39 06 555 0000`}
            onChange={onChange}
          />
        </div>
      </DialogBody>
      <DialogFooter>
        <DialogClose render={<Button type="button" variant="secondary" />} onClick={onCancel}>
          <Trans>Cancel</Trans>
        </DialogClose>
        <Button type="button" onClick={onNext}>
          <Trans>Next</Trans>
          <ChevronRightIcon />
        </Button>
      </DialogFooter>
    </DialogForm>
  );
}
