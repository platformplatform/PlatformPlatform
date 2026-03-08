import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Button } from "@repo/ui/components/Button";
import { DialogClose, DialogFooter } from "@repo/ui/components/Dialog";
import { Separator } from "@repo/ui/components/Separator";
import { Skeleton } from "@repo/ui/components/Skeleton";
import { formatCurrency } from "@repo/utils/currency/formatCurrency";
import { PaymentElement, useCheckout } from "@stripe/react-stripe-js/checkout";
import { useEffect, useState } from "react";
import { toast } from "sonner";

import { api, type SubscriptionPlan as SubscriptionPlanType } from "@/shared/lib/api/client";

import { getPlanDetails } from "./planUtils";

interface CheckoutFormProps {
  plan: SubscriptionPlanType;
  onConfirmed: () => void;
  onError: (error: string) => void;
}

function getDisplayError(message: string | undefined): string {
  if (!message) {
    return t`An error occurred while processing your payment.`;
  }
  return message;
}

export function CheckoutForm({ plan, onConfirmed, onError }: Readonly<CheckoutFormProps>) {
  const checkoutResult = useCheckout();
  const [isConfirming, setIsConfirming] = useState(false);
  const [isPaymentReady, setIsPaymentReady] = useState(false);

  const { data: subscription } = api.useQuery("get", "/api/account/subscriptions/current");
  const { data: preview } = api.useQuery("get", "/api/account/subscriptions/checkout-preview", {
    params: { query: { Plan: plan } }
  });

  const planDetails = getPlanDetails(plan);
  const checkoutError = checkoutResult.type === "error" ? checkoutResult.error : null;

  useEffect(() => {
    if (checkoutError) {
      console.error("[Checkout] useCheckout() error:", checkoutError);
      const errorMessage = getDisplayError(checkoutError.message);
      onError(errorMessage);
      toast.error(errorMessage);
    }
  }, [checkoutError, onError]);

  const handleSubmit = async () => {
    if (checkoutResult.type !== "success") {
      return;
    }

    setIsConfirming(true);
    onError("");

    const billingInfo = subscription?.billingInfo;
    const billingContact = {
      name: billingInfo?.name,
      address: {
        country: billingInfo?.address?.country ?? "",
        line1: billingInfo?.address?.line1,
        line2: billingInfo?.address?.line2 ?? null,
        city: billingInfo?.address?.city,
        // biome-ignore lint/style/useNamingConvention: Stripe API requires snake_case
        postal_code: billingInfo?.address?.postalCode,
        state: billingInfo?.address?.state ?? null
      }
    };

    try {
      await checkoutResult.checkout.updateBillingAddress(billingContact);

      const result = await checkoutResult.checkout.confirm({
        redirect: "if_required",
        returnUrl: window.location.href
      });

      if (result.type === "error") {
        setIsConfirming(false);
        console.error("[Checkout] confirm() error:", result.error);
        const errorMessage = getDisplayError(result.error.message);
        onError(errorMessage);
        toast.error(errorMessage);
        return;
      }

      setIsConfirming(false);
      onConfirmed();
    } catch (error) {
      setIsConfirming(false);
      console.error("[Checkout] confirm() exception:", error);
      const message = error instanceof Error ? error.message : undefined;
      const errorMessage = getDisplayError(message);
      onError(errorMessage);
      toast.error(errorMessage);
    }
  };

  const isCheckoutReady = checkoutResult.type === "success";

  return (
    <>
      <CheckoutSummary preview={preview} planName={planDetails.name} />
      {checkoutError ? (
        <DialogFooter>
          <DialogClose render={<Button type="reset" variant="secondary" />}>
            <Trans>Close</Trans>
          </DialogClose>
        </DialogFooter>
      ) : (
        <>
          <PaymentElement
            options={{ fields: { billingDetails: { name: "never" } } }}
            onReady={() => setIsPaymentReady(true)}
          />
          {isPaymentReady && (
            <>
              <p className="text-xs text-muted-foreground">
                <Trans>
                  By subscribing, you agree to our{" "}
                  <a href="/legal/terms" className="underline" target="_blank" rel="noopener noreferrer">
                    terms of service
                  </a>{" "}
                  and{" "}
                  <a href="/legal/privacy" className="underline" target="_blank" rel="noopener noreferrer">
                    privacy policy
                  </a>
                  .
                </Trans>
              </p>
              <DialogFooter>
                <DialogClose render={<Button type="reset" variant="secondary" disabled={isConfirming} />}>
                  <Trans>Cancel</Trans>
                </DialogClose>
                <Button onClick={handleSubmit} disabled={isConfirming || !isCheckoutReady || !preview}>
                  {isConfirming ? <Trans>Processing payment...</Trans> : <Trans>Pay and subscribe</Trans>}
                </Button>
              </DialogFooter>
            </>
          )}
        </>
      )}
    </>
  );
}

function CheckoutSummary({
  preview,
  planName
}: Readonly<{
  preview: { totalAmount: number; taxAmount: number; currency: string } | undefined;
  planName: string;
}>) {
  return (
    <div className="mb-2 flex flex-col gap-2">
      {preview ? (
        <>
          <div className="flex items-baseline justify-between gap-4 text-sm">
            <span className="text-muted-foreground">{planName}</span>
            <span className="shrink-0 whitespace-nowrap text-muted-foreground tabular-nums">
              {formatCurrency(preview.totalAmount - preview.taxAmount, preview.currency)}
            </span>
          </div>
          {preview.taxAmount > 0 && (
            <div className="flex items-baseline justify-between gap-4 text-sm">
              <span className="text-muted-foreground">
                <Trans>Tax</Trans>
              </span>
              <span className="shrink-0 whitespace-nowrap text-muted-foreground tabular-nums">
                {formatCurrency(preview.taxAmount, preview.currency)}
              </span>
            </div>
          )}
          <Separator />
          <div className="flex items-baseline justify-between gap-4 font-medium">
            <span>
              <Trans>Total</Trans>
            </span>
            <span className="shrink-0 text-lg whitespace-nowrap tabular-nums">
              {formatCurrency(preview.totalAmount, preview.currency)}
            </span>
          </div>
        </>
      ) : (
        <>
          <div className="flex items-center justify-between">
            <Skeleton className="h-4 w-[10rem]" />
            <Skeleton className="h-4 w-[4rem]" />
          </div>
          <Separator />
          <div className="flex items-center justify-between">
            <Skeleton className="h-5 w-[6rem]" />
            <Skeleton className="h-5 w-[5rem]" />
          </div>
        </>
      )}
    </div>
  );
}
