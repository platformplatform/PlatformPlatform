import { i18n } from "@lingui/core";
import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Button } from "@repo/ui/components/Button";
import {
  Dialog,
  DialogBody,
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle
} from "@repo/ui/components/Dialog";
import { Separator } from "@repo/ui/components/Separator";
import { Skeleton } from "@repo/ui/components/Skeleton";
import { formatCurrency } from "@repo/utils/currency/formatCurrency";
import { CheckoutProvider, PaymentElement, useCheckout } from "@stripe/react-stripe-js/checkout";
import { loadStripe, type Stripe } from "@stripe/stripe-js";
import { LoaderCircleIcon } from "lucide-react";
import { useEffect, useMemo, useState } from "react";
import { toast } from "sonner";
import { api, type SubscriptionPlan as SubscriptionPlanType } from "@/shared/lib/api/client";
import { getPlanDetails } from "./PlanCard";
import { getStripeAppearance } from "./stripeAppearance";

const ActivationPollingIntervalMs = 1000;
const ActivationTimeoutMs = 15_000;

interface CheckoutDialogProps {
  isOpen: boolean;
  onOpenChange: (isOpen: boolean) => void;
  plan: SubscriptionPlanType;
  prefetchedClientSecret?: string;
  prefetchedPublishableKey?: string;
}

export function CheckoutDialog({
  isOpen,
  onOpenChange,
  plan,
  prefetchedClientSecret,
  prefetchedPublishableKey
}: Readonly<CheckoutDialogProps>) {
  const [stripePromise, setStripePromise] = useState<Promise<Stripe | null> | null>(null);
  const [clientSecret, setClientSecret] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [paymentError, setPaymentError] = useState<string | null>(null);
  const [isWaitingForActivation, setIsWaitingForActivation] = useState(false);

  const { data: subscription } = api.useQuery(
    "get",
    "/api/account/subscriptions/current",
    {},
    { refetchInterval: isWaitingForActivation ? ActivationPollingIntervalMs : false }
  );

  useEffect(() => {
    if (!isWaitingForActivation || !subscription?.hasStripeSubscription) {
      return;
    }
    setIsWaitingForActivation(false);
    toast.success(t`Your subscription has been activated.`);
    onOpenChange(false);
  }, [isWaitingForActivation, subscription?.hasStripeSubscription, onOpenChange]);

  useEffect(() => {
    if (!isWaitingForActivation) {
      return;
    }
    const timeout = setTimeout(() => {
      setIsWaitingForActivation(false);
      toast.success(t`Your subscription has been activated.`);
      onOpenChange(false);
    }, ActivationTimeoutMs);
    return () => clearTimeout(timeout);
  }, [isWaitingForActivation, onOpenChange]);

  const checkoutMutation = api.useMutation("post", "/api/account/subscriptions/start-checkout", {
    onSuccess: (data) => {
      setClientSecret(data.clientSecret ?? null);
      if (data.publishableKey) {
        setStripePromise(loadStripe(data.publishableKey, { locale: i18n.locale as "auto" }));
      }
      setIsLoading(false);
    },
    onError: () => {
      setIsLoading(false);
    }
  });

  useEffect(() => {
    if (isOpen) {
      setPaymentError(null);
      if (prefetchedClientSecret && prefetchedPublishableKey) {
        setClientSecret(prefetchedClientSecret);
        setStripePromise(loadStripe(prefetchedPublishableKey, { locale: i18n.locale as "auto" }));
        setIsLoading(false);
      } else {
        setIsLoading(true);
        setClientSecret(null);
        setStripePromise(null);
        checkoutMutation.mutate({
          body: { plan }
        });
      }
    } else {
      setClientSecret(null);
      setStripePromise(null);
      setIsLoading(false);
      setPaymentError(null);
      setIsWaitingForActivation(false);
    }
  }, [isOpen]);

  const handleConfirmed = () => {
    setIsWaitingForActivation(true);
  };

  const checkoutOptions = useMemo(() => {
    if (!clientSecret) {
      return undefined;
    }
    return {
      clientSecret,
      elementsOptions: {
        appearance: getStripeAppearance()
      }
    };
  }, [clientSecret]);

  const isReady = stripePromise && checkoutOptions;

  return (
    <Dialog
      open={isOpen}
      onOpenChange={isWaitingForActivation ? undefined : onOpenChange}
      disablePointerDismissal={true}
      trackingTitle="Checkout"
    >
      <DialogContent className="sm:w-dialog-md">
        <DialogHeader>
          <DialogTitle>
            {isWaitingForActivation ? <Trans>Activating subscription</Trans> : <Trans>Subscribe</Trans>}
          </DialogTitle>
          <DialogDescription>
            {isWaitingForActivation ? (
              <Trans>Please wait while we confirm your payment. This may take a few moments.</Trans>
            ) : (
              <Trans>Complete your payment to activate your subscription.</Trans>
            )}
          </DialogDescription>
        </DialogHeader>
        <DialogBody>
          {isWaitingForActivation ? (
            <div className="flex flex-col items-center gap-4 py-8">
              <LoaderCircleIcon className="size-8 animate-spin text-primary" />
              <p className="text-muted-foreground text-sm">
                <Trans>Activating your subscription...</Trans>
              </p>
            </div>
          ) : (
            <>
              {isLoading && <CheckoutSkeleton />}
              {paymentError && <div className="text-destructive text-sm">{paymentError}</div>}
              {isReady && (
                <CheckoutProvider stripe={stripePromise} options={checkoutOptions}>
                  <CheckoutForm plan={plan} onConfirmed={handleConfirmed} onError={setPaymentError} />
                </CheckoutProvider>
              )}
            </>
          )}
        </DialogBody>
        {!isReady && !isWaitingForActivation && (
          <DialogFooter>
            <DialogClose render={<Button type="reset" variant="secondary" />}>
              <Trans>Cancel</Trans>
            </DialogClose>
          </DialogFooter>
        )}
      </DialogContent>
    </Dialog>
  );
}

function CheckoutSkeleton() {
  return (
    <div className="flex flex-col gap-4">
      <Skeleton className="h-[2.75rem] w-full" />
      <Skeleton className="h-[2.75rem] w-full" />
      <Skeleton className="h-[2.75rem] w-full" />
    </div>
  );
}

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

function CheckoutForm({ plan, onConfirmed, onError }: Readonly<CheckoutFormProps>) {
  const checkoutResult = useCheckout();
  const [isConfirming, setIsConfirming] = useState(false);
  const [isPaymentReady, setIsPaymentReady] = useState(false);

  const { data: subscription } = api.useQuery("get", "/api/account/subscriptions/current");
  const { data: preview } = api.useQuery("get", "/api/account/subscriptions/checkout-preview", {
    params: { query: { Plan: plan } }
  });

  const planDetails = getPlanDetails(plan);
  const hasCheckoutError = checkoutResult.type === "error";

  useEffect(() => {
    if (hasCheckoutError) {
      console.error("[Checkout] useCheckout() error:", checkoutResult.error);
      const errorMessage = getDisplayError(checkoutResult.error.message);
      onError(errorMessage);
      toast.error(errorMessage);
    }
  }, [hasCheckoutError]);

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
      <div className="mb-2 flex flex-col gap-2">
        {preview ? (
          <>
            <div className="flex items-baseline justify-between gap-4 text-sm">
              <span className="text-muted-foreground">{planDetails.name}</span>
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
              <span className="shrink-0 whitespace-nowrap text-lg tabular-nums">
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
      {hasCheckoutError ? (
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
              <p className="text-muted-foreground text-xs">
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
