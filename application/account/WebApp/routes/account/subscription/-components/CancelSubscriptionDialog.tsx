import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogBody,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogMedia,
  AlertDialogTitle
} from "@repo/ui/components/AlertDialog";
import { Field, FieldContent, FieldLabel, FieldTitle } from "@repo/ui/components/Field";
import { RadioGroup, RadioGroupItem } from "@repo/ui/components/RadioGroup";
import { TextAreaField } from "@repo/ui/components/TextAreaField";
import { useFormatLongDate } from "@repo/ui/hooks/useSmartDate";
import { AlertTriangleIcon } from "lucide-react";
import { useState } from "react";
import { CancellationReason } from "@/shared/lib/api/client";

type CancelSubscriptionDialogProps = {
  isOpen: boolean;
  onOpenChange: (isOpen: boolean) => void;
  onConfirm: (reason: CancellationReason, feedback: string | null) => void;
  isPending: boolean;
  currentPeriodEnd: string | null;
};

export function CancelSubscriptionDialog({
  isOpen,
  onOpenChange,
  onConfirm,
  isPending,
  currentPeriodEnd
}: Readonly<CancelSubscriptionDialogProps>) {
  const formatLongDate = useFormatLongDate();
  const formattedDate = formatLongDate(currentPeriodEnd);
  const [reason, setReason] = useState<CancellationReason | null>(null);
  const [feedback, setFeedback] = useState("");

  function handleOpenChange(open: boolean) {
    if (!open) {
      setReason(null);
      setFeedback("");
    }
    onOpenChange(open);
  }

  return (
    <AlertDialog open={isOpen} onOpenChange={handleOpenChange} trackingTitle="Cancel subscription">
      <AlertDialogContent className="max-h-[calc(100vh-2rem)] grid-rows-[auto_1fr_auto] sm:w-dialog-md">
        <AlertDialogHeader>
          <AlertDialogMedia className="bg-destructive/10">
            <AlertTriangleIcon className="text-destructive" />
          </AlertDialogMedia>
          <AlertDialogTitle>{t`Cancel subscription`}</AlertDialogTitle>
          <AlertDialogDescription>
            {formattedDate ? (
              <Trans>
                Your subscription will remain active until {formattedDate}. After that, your account will switch to the
                free plan.
              </Trans>
            ) : (
              <Trans>
                Your subscription will be cancelled at the end of the current billing period. After that, your account
                will switch to the free plan.
              </Trans>
            )}
          </AlertDialogDescription>
        </AlertDialogHeader>

        <AlertDialogBody>
          <RadioGroup
            aria-label={t`Cancellation reason`}
            value={reason ?? ""}
            onValueChange={(value) => setReason(value as CancellationReason)}
          >
            <FieldLabel>
              <Field orientation="horizontal">
                <RadioGroupItem value={CancellationReason.FoundAlternative} aria-label={t`Found an alternative`} />
                <FieldContent>
                  <FieldTitle>
                    <Trans>Found an alternative</Trans>
                  </FieldTitle>
                </FieldContent>
              </Field>
            </FieldLabel>
            <FieldLabel>
              <Field orientation="horizontal">
                <RadioGroupItem value={CancellationReason.TooExpensive} aria-label={t`Too expensive`} />
                <FieldContent>
                  <FieldTitle>
                    <Trans>Too expensive</Trans>
                  </FieldTitle>
                </FieldContent>
              </Field>
            </FieldLabel>
            <FieldLabel>
              <Field orientation="horizontal">
                <RadioGroupItem value={CancellationReason.NoLongerNeeded} aria-label={t`No longer needed`} />
                <FieldContent>
                  <FieldTitle>
                    <Trans>No longer needed</Trans>
                  </FieldTitle>
                </FieldContent>
              </Field>
            </FieldLabel>
            <FieldLabel>
              <Field orientation="horizontal">
                <RadioGroupItem value={CancellationReason.Other} aria-label={t`Other reason`} />
                <FieldContent>
                  <FieldTitle>
                    <Trans>Other reason</Trans>
                  </FieldTitle>
                </FieldContent>
              </Field>
            </FieldLabel>
          </RadioGroup>

          <TextAreaField
            name="feedback"
            label={t`Is there anything else you would like us to know? (optional)`}
            placeholder={t`Tell us more...`}
            value={feedback}
            onChange={setFeedback}
            isDisabled={isPending}
            maxLength={500}
            rows={5}
          />
        </AlertDialogBody>

        <AlertDialogFooter>
          <AlertDialogCancel disabled={isPending}>{t`Keep subscription`}</AlertDialogCancel>
          <AlertDialogAction
            variant="destructive"
            onClick={() => {
              if (reason !== null) {
                onConfirm(reason, feedback.trim() || null);
              }
            }}
            disabled={isPending || reason === null}
          >
            {isPending ? t`Cancelling...` : t`Cancel subscription`}
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  );
}
