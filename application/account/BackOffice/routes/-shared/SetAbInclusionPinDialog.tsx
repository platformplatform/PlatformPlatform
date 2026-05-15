import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Button } from "@repo/ui/components/Button";
import {
  DialogBody,
  DialogClose,
  DialogContent,
  DialogFooter,
  DialogForm,
  DialogHeader,
  DialogTitle
} from "@repo/ui/components/Dialog";
import { DirtyDialog } from "@repo/ui/components/DirtyDialog";
import { useDialogSetDirty } from "@repo/ui/components/DirtyDialogContext";
import { Field, FieldContent, FieldDescription, FieldLabel, FieldTitle } from "@repo/ui/components/Field";
import { RadioGroup, RadioGroupItem } from "@repo/ui/components/RadioGroup";
import { useState } from "react";
import { toast } from "sonner";

import { AbInclusionPin, api, queryClient } from "@/shared/lib/api/client";

type Entity = "tenant" | "user";

// `default` (no pin) cannot be represented as a RadioGroup value of `null`, so we encode it as a
// distinct radio value. On submit we translate `default` back to the API's `null` body.
type RadioValue = AbInclusionPin | "default";

interface SetAbInclusionPinDialogProps {
  entity: Entity;
  entityId: string;
  entityLabel: string;
  currentPin: AbInclusionPin | null;
  isOpen: boolean;
  onOpenChange: (isOpen: boolean) => void;
}

export function SetAbInclusionPinDialog({
  entity,
  entityId,
  entityLabel,
  currentPin,
  isOpen,
  onOpenChange
}: Readonly<SetAbInclusionPinDialogProps>) {
  const handleClose = () => onOpenChange(false);
  return (
    <DirtyDialog open={isOpen} onOpenChange={onOpenChange} trackingTitle="Feature flag rollouts">
      <DialogContent className="sm:w-dialog-lg">
        <DialogHeader>
          <DialogTitle>
            <Trans>Feature flag rollouts</Trans>
          </DialogTitle>
        </DialogHeader>
        <SetAbInclusionPinDialogBody
          entity={entity}
          entityId={entityId}
          entityLabel={entityLabel}
          currentPin={currentPin}
          onClose={handleClose}
        />
      </DialogContent>
    </DirtyDialog>
  );
}

function SetAbInclusionPinDialogBody({
  entity,
  entityId,
  entityLabel,
  currentPin,
  onClose
}: Readonly<{
  entity: Entity;
  entityId: string;
  entityLabel: string;
  currentPin: AbInclusionPin | null;
  onClose: () => void;
}>) {
  const setDirty = useDialogSetDirty();
  const [selected, setSelected] = useState<RadioValue>(currentPin ?? "default");

  const tenantMutation = api.useMutation("put", "/api/back-office/tenants/{id}/ab-inclusion-pin");
  const userMutation = api.useMutation("put", "/api/back-office/users/{id}/ab-inclusion-pin");
  const mutation = entity === "tenant" ? tenantMutation : userMutation;

  const invalidate = () => {
    if (entity === "tenant") {
      queryClient.invalidateQueries({ queryKey: ["get", "/api/back-office/tenants/{id}"] });
    } else {
      queryClient.invalidateQueries({ queryKey: ["get", "/api/back-office/users/{id}"] });
    }
    queryClient.invalidateQueries({ queryKey: ["get", "/api/back-office/feature-flags/{flagKey}/tenants"] });
    queryClient.invalidateQueries({ queryKey: ["get", "/api/back-office/feature-flags/{flagKey}/users"] });
    queryClient.invalidateQueries({ queryKey: ["get", "/api/back-office/tenants/{id}/feature-flags"] });
    queryClient.invalidateQueries({ queryKey: ["get", "/api/back-office/users/{id}/feature-flags"] });
  };

  const handleSubmit = () => {
    const pin: AbInclusionPin | null = selected === "default" ? null : selected;
    const onSuccess = () => {
      const message =
        pin === AbInclusionPin.AlwaysOn
          ? t`${entityLabel} is now first in feature flag rollouts`
          : pin === AbInclusionPin.NeverOn
            ? t`${entityLabel} is now last in feature flag rollouts`
            : t`Feature flag rollouts reset to default for ${entityLabel}`;
      toast.success(message);
      invalidate();
      onClose();
    };
    if (entity === "tenant") {
      tenantMutation.mutate({ params: { path: { id: entityId } }, body: { abInclusionPin: pin } }, { onSuccess });
    } else {
      userMutation.mutate({ params: { path: { id: entityId } }, body: { abInclusionPin: pin } }, { onSuccess });
    }
  };

  return (
    <DialogForm onSubmit={handleSubmit} validationErrors={mutation.error?.errors}>
      <DialogBody>
        <p className="text-sm text-muted-foreground">
          <Trans>
            Choose how {entityLabel} participates in feature flag rollouts. Per-flag manual overrides still take
            precedence over this setting.
          </Trans>
        </p>

        <RadioGroup
          aria-label={t`Feature flag rollouts`}
          value={selected}
          onValueChange={(value) => {
            setSelected(value as RadioValue);
            setDirty(true);
          }}
          className="mt-3"
        >
          <FieldLabel>
            <Field orientation="horizontal">
              <RadioGroupItem value="default" id="pin-default" autoFocus={true} />
              <FieldContent>
                <FieldTitle>
                  <Trans>Default</Trans>
                </FieldTitle>
                <FieldDescription>
                  <Trans>Included in feature flag rollouts based on the assigned rollout bucket.</Trans>
                </FieldDescription>
              </FieldContent>
            </Field>
          </FieldLabel>
          <FieldLabel>
            <Field orientation="horizontal">
              <RadioGroupItem value={AbInclusionPin.AlwaysOn} id="pin-always" />
              <FieldContent>
                <FieldTitle>
                  <Trans>First in feature flag rollouts</Trans>
                </FieldTitle>
                <FieldDescription>
                  <Trans>Included at 1% — the first to receive any feature flag rollout.</Trans>
                </FieldDescription>
              </FieldContent>
            </Field>
          </FieldLabel>
          <FieldLabel>
            <Field orientation="horizontal">
              <RadioGroupItem value={AbInclusionPin.NeverOn} id="pin-never" />
              <FieldContent>
                <FieldTitle>
                  <Trans>Last in feature flag rollouts</Trans>
                </FieldTitle>
                <FieldDescription>
                  <Trans>Included at 100% — the last to receive any feature flag rollout.</Trans>
                </FieldDescription>
              </FieldContent>
            </Field>
          </FieldLabel>
        </RadioGroup>
      </DialogBody>
      <DialogFooter>
        <DialogClose render={<Button type="reset" variant="secondary" disabled={mutation.isPending} />}>
          <Trans>Cancel</Trans>
        </DialogClose>
        <Button type="submit" isPending={mutation.isPending}>
          {mutation.isPending ? <Trans>Saving...</Trans> : <Trans>Save changes</Trans>}
        </Button>
      </DialogFooter>
    </DialogForm>
  );
}
