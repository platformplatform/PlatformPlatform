import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Button } from "@repo/ui/components/Button";
import { CheckboxField } from "@repo/ui/components/CheckboxField";
import { DialogBody, DialogFooter, DialogForm } from "@repo/ui/components/Dialog";
import { Field, FieldContent, FieldDescription, FieldLabel, FieldTitle } from "@repo/ui/components/Field";
import { InlineFieldGroup } from "@repo/ui/components/InlineFieldGroup";
import { RadioGroup, RadioGroupItem } from "@repo/ui/components/RadioGroup";
import { mutationSubmitter } from "@repo/ui/forms/mutationSubmitter";
import { type UseMutationResult } from "@tanstack/react-query";
import { ChefHatIcon, ChevronLeftIcon, FlameIcon, UtensilsIcon } from "lucide-react";
import { useState } from "react";

interface CategorizationStepProps {
  mutation: UseMutationResult<void, unknown, { body?: unknown }>;
  onBack: () => void;
  onChange: () => void;
}

export function CategorizationStep({ mutation, onBack, onChange }: Readonly<CategorizationStepProps>) {
  const [favoriteChecked, setFavoriteChecked] = useState(false);

  return (
    <DialogForm onSubmit={mutationSubmitter(mutation)}>
      <DialogBody>
        <div className="flex flex-col gap-4">
          <RadioGroup defaultValue="medium" onValueChange={onChange}>
            <FieldLabel>
              <Field orientation="horizontal">
                <RadioGroupItem value="easy" />
                <FieldContent>
                  <FieldTitle>
                    <UtensilsIcon />
                    <Trans>Easy</Trans>
                  </FieldTitle>
                  <FieldDescription>
                    <Trans>Basic skills, under 30 minutes</Trans>
                  </FieldDescription>
                </FieldContent>
              </Field>
            </FieldLabel>
            <FieldLabel>
              <Field orientation="horizontal">
                <RadioGroupItem value="medium" />
                <FieldContent>
                  <FieldTitle>
                    <ChefHatIcon />
                    <Trans>Medium</Trans>
                  </FieldTitle>
                  <FieldDescription>
                    <Trans>Some cooking experience, 30 to 60 minutes</Trans>
                  </FieldDescription>
                </FieldContent>
              </Field>
            </FieldLabel>
            <FieldLabel>
              <Field orientation="horizontal">
                <RadioGroupItem value="hard" />
                <FieldContent>
                  <FieldTitle>
                    <FlameIcon />
                    <Trans>Hard</Trans>
                  </FieldTitle>
                  <FieldDescription>
                    <Trans>Advanced techniques, over 60 minutes</Trans>
                  </FieldDescription>
                </FieldContent>
              </Field>
            </FieldLabel>
          </RadioGroup>
          <InlineFieldGroup>
            <CheckboxField
              name="favorite"
              label={t`Add to favorites`}
              checked={favoriteChecked}
              onCheckedChange={(v) => {
                setFavoriteChecked(!!v);
                onChange();
              }}
            />
          </InlineFieldGroup>
        </div>
      </DialogBody>
      <DialogFooter>
        <Button type="button" variant="secondary" onClick={onBack} disabled={mutation.isPending}>
          <ChevronLeftIcon />
          <Trans>Back</Trans>
        </Button>
        <Button type="submit" isPending={mutation.isPending}>
          {mutation.isPending ? <Trans>Saving...</Trans> : <Trans>Save changes</Trans>}
        </Button>
      </DialogFooter>
    </DialogForm>
  );
}
