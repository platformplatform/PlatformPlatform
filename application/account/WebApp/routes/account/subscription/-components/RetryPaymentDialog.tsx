import { i18n } from "@lingui/core";
import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Button } from "@repo/ui/components/Button";
import {
  Dialog,
  DialogBody,
  DialogClose,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle
} from "@repo/ui/components/Dialog";
import { Separator } from "@repo/ui/components/Separator";
import { formatCurrency } from "@repo/utils/currency/formatCurrency";
import { loadStripe } from "@stripe/stripe-js";
import { useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { toast } from "sonner";
import type { components } from "@/shared/lib/api/api.generated";
import { api } from "@/shared/lib/api/client";
import { BillingInfoDisplay } from "./BillingInfoDisplay";
import { PaymentMethodDisplay } from "./PaymentMethodDisplay";

type BillingInfo = components["schemas"]["BillingInfo"];
type PaymentMethod = components["schemas"]["PaymentMethod"];

interface RetryPaymentDialogProps {
  isOpen: boolean;
  onOpenChange: (isOpen: boolean) => void;
  billingInfo: BillingInfo | null | undefined;
  paymentMethod: PaymentMethod | null | undefined;
  amount: number;
  currency: string;
}

export function RetryPaymentDialog({
  isOpen,
  onOpenChange,
  billingInfo,
  paymentMethod,
  amount,
  currency
}: Readonly<RetryPaymentDialogProps>) {
  const queryClient = useQueryClient();
  const [isConfirmingPayment, setIsConfirmingPayment] = useState(false);

  const retryMutation = api.useMutation("post", "/api/account/subscriptions/retry-pending-invoice", {
    onSuccess: async (data) => {
      if (data.clientSecret && data.publishableKey) {
        setIsConfirmingPayment(true);
        const paymentStripe = await loadStripe(data.publishableKey, { locale: i18n.locale as "auto" });
        if (!paymentStripe) {
          setIsConfirmingPayment(false);
          toast.error(t`Failed to load payment processor.`);
          return;
        }
        const result = await paymentStripe.confirmPayment({
          clientSecret: data.clientSecret,
          confirmParams: {
            // biome-ignore lint/style/useNamingConvention: Stripe API uses snake_case
            return_url: window.location.href
          },
          redirect: "if_required"
        });
        setIsConfirmingPayment(false);
        if (result.error) {
          toast.error(result.error.message ?? t`Payment authentication failed.`);
          return;
        }
        queryClient.invalidateQueries();
        toast.success(t`Pending payment completed`);
      } else if (data.paid) {
        queryClient.invalidateQueries();
        toast.success(t`Pending payment completed`);
      } else {
        toast.error(t`Payment could not be processed. Please try again later.`);
        return;
      }
      onOpenChange(false);
    }
  });

  const isPending = retryMutation.isPending || isConfirmingPayment;

  return (
    <Dialog open={isOpen} onOpenChange={onOpenChange} disablePointerDismissal={true} trackingTitle="Retry payment">
      <DialogContent className="sm:w-dialog-md">
        <DialogHeader>
          <DialogTitle>
            <Trans>Retry pending payment</Trans>
          </DialogTitle>
        </DialogHeader>
        <DialogBody>
          <div className="flex flex-col gap-4">
            <div className="flex flex-col gap-2">
              <span className="font-medium text-sm">
                <Trans>Bill to</Trans>
              </span>
              <BillingInfoDisplay billingInfo={billingInfo} />
            </div>

            <Separator />

            <div className="flex flex-col gap-2">
              <span className="font-medium text-sm">
                <Trans>Payment method</Trans>
              </span>
              <PaymentMethodDisplay paymentMethod={paymentMethod} />
            </div>

            <Separator />

            <div className="flex items-baseline justify-between gap-4 font-medium">
              <span>
                <Trans>Total</Trans>
              </span>
              <span className="shrink-0 whitespace-nowrap text-lg tabular-nums">
                {formatCurrency(amount, currency)}
              </span>
            </div>
          </div>
        </DialogBody>
        <DialogFooter>
          <DialogClose render={<Button type="reset" variant="secondary" disabled={isPending} />}>
            <Trans>Cancel</Trans>
          </DialogClose>
          <Button onClick={() => retryMutation.mutate({})} disabled={isPending}>
            {isPending ? <Trans>Processing...</Trans> : <Trans>Pay</Trans>}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
