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
import { Skeleton } from "@repo/ui/components/Skeleton";
import { CheckoutProvider, PaymentElement, useCheckout } from "@stripe/react-stripe-js/checkout";
import { loadStripe, type Stripe } from "@stripe/stripe-js";
import { useQueryClient } from "@tanstack/react-query";
import { useEffect, useMemo, useState } from "react";
import { toast } from "sonner";
import { api, SubscriptionPlan, type SubscriptionPlan as SubscriptionPlanType } from "@/shared/lib/api/client";
import { getStripeAppearance } from "./stripeAppearance";

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

  const checkoutMutation = api.useMutation("post", "/api/account/subscriptions/checkout", {
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
          body: {
            plan,
            returnUrl: `${window.location.origin}/account/subscription/?session_id={CHECKOUT_SESSION_ID}`
          }
        });
      }
    } else {
      setClientSecret(null);
      setStripePromise(null);
      setIsLoading(false);
      setPaymentError(null);
    }
  }, [isOpen]);

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
    <Dialog open={isOpen} onOpenChange={onOpenChange} trackingTitle="Checkout">
      <DialogContent className="sm:w-dialog-md">
        <DialogHeader>
          <DialogTitle>
            <Trans>Subscribe</Trans>
          </DialogTitle>
          <DialogDescription>
            <Trans>Complete your payment to activate your subscription.</Trans>
          </DialogDescription>
        </DialogHeader>
        <DialogBody>
          {isLoading && <CheckoutSkeleton />}
          {paymentError && <div className="text-destructive text-sm">{paymentError}</div>}
          {isReady && (
            <CheckoutProvider stripe={stripePromise} options={checkoutOptions}>
              <CheckoutForm plan={plan} onSuccess={() => onOpenChange(false)} onError={setPaymentError} />
            </CheckoutProvider>
          )}
        </DialogBody>
        {!isReady && (
          <DialogFooter>
            <DialogClose render={<Button variant="secondary" />}>
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

function getPlanSummary(plan: SubscriptionPlanType): { name: string; price: string } {
  switch (plan) {
    case SubscriptionPlan.Standard:
      return { name: t`Standard`, price: t`EUR 19/month` };
    case SubscriptionPlan.Premium:
      return { name: t`Premium`, price: t`EUR 39/month` };
    default:
      return { name: t`Basis`, price: t`Free` };
  }
}

interface CheckoutFormProps {
  plan: SubscriptionPlanType;
  onSuccess: () => void;
  onError: (error: string) => void;
}

const WebhookPollIntervalMs = 1000;
const WebhookTimeoutMs = 15000;

function getDisplayError(message: string | undefined): string {
  if (!message) {
    return t`An error occurred while processing your payment.`;
  }
  if (/billing.*(address|info)/i.test(message)) {
    return t`Please update your billing information before subscribing.`;
  }
  return message;
}

function CheckoutForm({ plan, onSuccess, onError }: Readonly<CheckoutFormProps>) {
  const checkoutResult = useCheckout();
  const queryClient = useQueryClient();
  const [isConfirming, setIsConfirming] = useState(false);
  const [isPaymentReady, setIsPaymentReady] = useState(false);
  const [isWaitingForWebhook, setIsWaitingForWebhook] = useState(false);

  const { data: subscription } = api.useQuery(
    "get",
    "/api/account/subscriptions/current",
    {},
    { refetchInterval: isWaitingForWebhook ? WebhookPollIntervalMs : false }
  );

  useEffect(() => {
    if (!isWaitingForWebhook) {
      return;
    }

    if (subscription?.hasStripeSubscription) {
      setIsWaitingForWebhook(false);
      queryClient.invalidateQueries({ queryKey: ["get", "/api/account/subscriptions/current"] });
      toast.success(t`Your subscription has been activated.`);
      onSuccess();
    }
  }, [isWaitingForWebhook, subscription?.hasStripeSubscription, queryClient, onSuccess]);

  useEffect(() => {
    if (!isWaitingForWebhook) {
      return;
    }

    const timeout = setTimeout(() => {
      setIsWaitingForWebhook(false);
      queryClient.invalidateQueries({ queryKey: ["get", "/api/account/subscriptions/current"] });
      toast.success(t`Your subscription has been activated.`);
      onSuccess();
    }, WebhookTimeoutMs);

    return () => clearTimeout(timeout);
  }, [isWaitingForWebhook, queryClient, onSuccess]);

  const isPending = isConfirming || isWaitingForWebhook;
  const planSummary = getPlanSummary(plan);
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
    const billingAddress = billingInfo?.address
      ? {
          address: {
            country: billingInfo.address.country ?? "",
            line1: billingInfo.address.line1 ?? "",
            line2: billingInfo.address.line2 ?? null,
            city: billingInfo.address.city ?? "",
            // biome-ignore lint/style/useNamingConvention: Stripe API requires snake_case
            postal_code: billingInfo.address.postalCode ?? "",
            state: billingInfo.address.state ?? null
          }
        }
      : undefined;

    try {
      const result = await checkoutResult.checkout.confirm({
        redirect: "if_required",
        billingAddress
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
      setIsWaitingForWebhook(true);
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
      <div className="flex items-center justify-between rounded-lg border border-border bg-muted/50 p-4">
        <span className="font-medium">{planSummary.name}</span>
        <span className="font-semibold">{planSummary.price}</span>
      </div>
      {hasCheckoutError ? (
        <DialogFooter>
          <DialogClose render={<Button variant="secondary" />}>
            <Trans>Close</Trans>
          </DialogClose>
        </DialogFooter>
      ) : (
        <>
          <PaymentElement onReady={() => setIsPaymentReady(true)} />
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
                <DialogClose render={<Button type="reset" variant="secondary" disabled={isPending} />}>
                  <Trans>Cancel</Trans>
                </DialogClose>
                <Button onClick={handleSubmit} disabled={isPending || !isCheckoutReady}>
                  {isPending ? <Trans>Processing...</Trans> : <Trans>Subscribe</Trans>}
                </Button>
              </DialogFooter>
            </>
          )}
        </>
      )}
    </>
  );
}
