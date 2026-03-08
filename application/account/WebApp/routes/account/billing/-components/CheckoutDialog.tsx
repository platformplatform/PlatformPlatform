import type { Stripe } from "@stripe/stripe-js";

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
import { CheckoutProvider } from "@stripe/react-stripe-js/checkout";
import { loadStripe } from "@stripe/stripe-js/pure";
import { LoaderCircleIcon } from "lucide-react";
import { useEffect, useMemo, useState } from "react";
import { toast } from "sonner";

import { api, type SubscriptionPlan as SubscriptionPlanType } from "@/shared/lib/api/client";

import { CheckoutForm } from "./CheckoutForm";
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
  const startCheckout = checkoutMutation.mutate;

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
        startCheckout({
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
  }, [isOpen, prefetchedClientSecret, prefetchedPublishableKey, plan, startCheckout]);

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
              <p className="text-sm text-muted-foreground">
                <Trans>Activating your subscription...</Trans>
              </p>
            </div>
          ) : (
            <>
              {isLoading && <CheckoutSkeleton />}
              {paymentError && <div className="text-sm text-destructive">{paymentError}</div>}
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
