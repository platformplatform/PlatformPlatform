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
import { EmbeddedCheckout, EmbeddedCheckoutProvider } from "@stripe/react-stripe-js";
import { loadStripe, type Stripe } from "@stripe/stripe-js";
import { useQueryClient } from "@tanstack/react-query";
import { useEffect, useMemo, useState } from "react";
import { toast } from "sonner";
import { api, type SubscriptionPlan } from "@/shared/lib/api/client";

interface CheckoutDialogProps {
  isOpen: boolean;
  onOpenChange: (isOpen: boolean) => void;
  plan: SubscriptionPlan;
  prefetchedClientSecret?: string;
  prefetchedPublishableKey?: string;
}

const WebhookPollIntervalMs = 1000;
const WebhookTimeoutMs = 15000;

export function CheckoutDialog({
  isOpen,
  onOpenChange,
  plan,
  prefetchedClientSecret,
  prefetchedPublishableKey
}: Readonly<CheckoutDialogProps>) {
  const queryClient = useQueryClient();
  const [stripePromise, setStripePromise] = useState<Promise<Stripe | null> | null>(null);
  const [clientSecret, setClientSecret] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [isWaitingForWebhook, setIsWaitingForWebhook] = useState(false);

  const checkoutMutation = api.useMutation("post", "/api/account/subscriptions/checkout", {
    onSuccess: (data) => {
      setClientSecret(data.clientSecret ?? null);
      if (data.publishableKey) {
        setStripePromise(loadStripe(data.publishableKey));
      }
      setIsLoading(false);
    },
    onError: () => {
      setIsLoading(false);
    }
  });

  const { data: subscription } = api.useQuery(
    "get",
    "/api/account/subscriptions/current",
    {},
    { refetchInterval: isWaitingForWebhook ? WebhookPollIntervalMs : false }
  );

  useEffect(() => {
    if (isOpen) {
      setIsWaitingForWebhook(false);
      if (prefetchedClientSecret && prefetchedPublishableKey) {
        setClientSecret(prefetchedClientSecret);
        setStripePromise(loadStripe(prefetchedPublishableKey));
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
      setIsWaitingForWebhook(false);
    }
  }, [isOpen]);

  useEffect(() => {
    if (!isWaitingForWebhook) {
      return;
    }

    if (subscription?.hasStripeSubscription) {
      setIsWaitingForWebhook(false);
      queryClient.invalidateQueries({ queryKey: ["get", "/api/account/subscriptions/current"] });
      toast.success(t`Your subscription has been activated.`);
      onOpenChange(false);
    }
  }, [isWaitingForWebhook, subscription?.hasStripeSubscription, queryClient, onOpenChange]);

  useEffect(() => {
    if (!isWaitingForWebhook) {
      return;
    }

    const timeout = setTimeout(() => {
      setIsWaitingForWebhook(false);
      queryClient.invalidateQueries({ queryKey: ["get", "/api/account/subscriptions/current"] });
      toast.success(t`Your subscription has been activated.`);
      onOpenChange(false);
    }, WebhookTimeoutMs);

    return () => clearTimeout(timeout);
  }, [isWaitingForWebhook, queryClient, onOpenChange]);

  const handleComplete = () => {
    setIsWaitingForWebhook(true);
  };

  const isReady = stripePromise && clientSecret;

  const zoomFactor = useMemo(() => {
    const rootFontSize = parseFloat(getComputedStyle(document.documentElement).fontSize);
    return rootFontSize / 16;
  }, []);

  return (
    <Dialog open={isOpen} onOpenChange={onOpenChange}>
      <DialogContent className="sm:w-dialog-lg">
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
          {isReady && (
            <div style={{ zoom: zoomFactor }}>
              <EmbeddedCheckoutProvider stripe={stripePromise} options={{ clientSecret, onComplete: handleComplete }}>
                <EmbeddedCheckout />
              </EmbeddedCheckoutProvider>
            </div>
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
