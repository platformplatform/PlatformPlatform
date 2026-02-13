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
import { Elements, PaymentElement, useElements, useStripe } from "@stripe/react-stripe-js";
import { loadStripe, type Stripe } from "@stripe/stripe-js";
import { useQueryClient } from "@tanstack/react-query";
import { useEffect, useMemo, useRef, useState } from "react";
import { toast } from "sonner";
import { api } from "@/shared/lib/api/client";
import { getStripeAppearance } from "./stripeAppearance";

interface UpdatePaymentMethodDialogProps {
  isOpen: boolean;
  onOpenChange: (isOpen: boolean) => void;
}

export function UpdatePaymentMethodDialog({ isOpen, onOpenChange }: Readonly<UpdatePaymentMethodDialogProps>) {
  const [stripePromise, setStripePromise] = useState<Promise<Stripe | null> | null>(null);
  const [clientSecret, setClientSecret] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [setupError, setSetupError] = useState<string | null>(null);

  const setupMutation = api.useMutation("post", "/api/account/subscriptions/payment-method-setup", {
    onSuccess: (data) => {
      setClientSecret(data.clientSecret);
      setStripePromise(loadStripe(data.publishableKey, { locale: i18n.locale as "auto" }));
      setIsLoading(false);
    },
    onError: () => {
      setIsLoading(false);
    }
  });

  useEffect(() => {
    if (isOpen) {
      setIsLoading(true);
      setSetupError(null);
      setClientSecret(null);
      setStripePromise(null);
      setupMutation.mutate({});
    } else {
      setClientSecret(null);
      setStripePromise(null);
      setSetupError(null);
      setIsLoading(false);
    }
  }, [isOpen]);

  const elementsOptions = useMemo(() => {
    if (!clientSecret) {
      return undefined;
    }
    return {
      clientSecret,
      appearance: getStripeAppearance()
    };
  }, [clientSecret]);

  const isReady = stripePromise && elementsOptions;

  return (
    <Dialog open={isOpen} onOpenChange={onOpenChange} trackingTitle="Update payment method">
      <DialogContent className="sm:w-dialog-md">
        <DialogHeader>
          <DialogTitle>
            <Trans>Update payment method</Trans>
          </DialogTitle>
          <DialogDescription>
            <Trans>Enter your new payment details below.</Trans>
          </DialogDescription>
        </DialogHeader>
        <DialogBody>
          {isLoading && <PaymentFormSkeleton />}
          {setupError && <div className="text-destructive text-sm">{setupError}</div>}
          {isReady && (
            <Elements stripe={stripePromise} options={elementsOptions}>
              <PaymentForm
                onSuccess={() => {
                  onOpenChange(false);
                }}
                onError={setSetupError}
              />
            </Elements>
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

function PaymentFormSkeleton() {
  return (
    <div className="flex flex-col gap-4">
      <Skeleton className="h-[2.75rem] w-full" />
      <Skeleton className="h-[2.75rem] w-full" />
      <Skeleton className="h-[2.75rem] w-full" />
    </div>
  );
}

interface PaymentFormProps {
  onSuccess: () => void;
  onError: (error: string) => void;
}

const WebhookPollIntervalMs = 1000;
const WebhookTimeoutMs = 15000;

function PaymentForm({ onSuccess, onError }: Readonly<PaymentFormProps>) {
  const stripe = useStripe();
  const elements = useElements();
  const queryClient = useQueryClient();
  const [isConfirming, setIsConfirming] = useState(false);
  const [isPaymentReady, setIsPaymentReady] = useState(false);
  const [isWaitingForWebhook, setIsWaitingForWebhook] = useState(false);
  const previousPaymentMethodRef = useRef<{ last4: string; brand: string } | null>(null);

  const { data: subscription } = api.useQuery(
    "get",
    "/api/account/subscriptions/current",
    {},
    { refetchInterval: isWaitingForWebhook ? WebhookPollIntervalMs : false }
  );

  const confirmMutation = api.useMutation("post", "/api/account/subscriptions/confirm-payment-method", {
    onSuccess: () => {
      const current = subscription?.paymentMethod;
      previousPaymentMethodRef.current = current ? { last4: current.last4, brand: current.brand } : null;
      setIsWaitingForWebhook(true);
    }
  });

  useEffect(() => {
    if (!isWaitingForWebhook) {
      return;
    }

    const current = subscription?.paymentMethod;
    const previous = previousPaymentMethodRef.current;
    const hasChanged = current?.last4 !== previous?.last4 || current?.brand !== previous?.brand;

    if (hasChanged) {
      setIsWaitingForWebhook(false);
      toast.success(t`Payment method updated`);
      onSuccess();
    }
  }, [isWaitingForWebhook, subscription?.paymentMethod, onSuccess]);

  useEffect(() => {
    if (!isWaitingForWebhook) {
      return;
    }

    const timeout = setTimeout(() => {
      setIsWaitingForWebhook(false);
      queryClient.invalidateQueries({ queryKey: ["get", "/api/account/subscriptions/current"] });
      toast.success(t`Payment method updated`);
      onSuccess();
    }, WebhookTimeoutMs);

    return () => clearTimeout(timeout);
  }, [isWaitingForWebhook, queryClient, onSuccess]);

  const isPending = isConfirming || confirmMutation.isPending || isWaitingForWebhook;

  const handleSubmit = async () => {
    if (!stripe || !elements) {
      return;
    }

    setIsConfirming(true);
    onError("");

    const result = await stripe.confirmSetup({
      elements,
      confirmParams: {
        // biome-ignore lint/style/useNamingConvention: Stripe API uses snake_case
        return_url: window.location.href
      },
      redirect: "if_required"
    });

    if (result.error) {
      setIsConfirming(false);
      onError(result.error.message ?? t`An error occurred while updating your payment method.`);
      return;
    }

    setIsConfirming(false);

    if (result.setupIntent) {
      confirmMutation.mutate({
        body: { setupIntentId: result.setupIntent.id }
      });
    }
  };

  return (
    <>
      <PaymentElement onReady={() => setIsPaymentReady(true)} />
      {isPaymentReady && (
        <DialogFooter>
          <DialogClose render={<Button type="reset" variant="secondary" disabled={isPending} />}>
            <Trans>Cancel</Trans>
          </DialogClose>
          <Button onClick={handleSubmit} disabled={isPending || !stripe || !elements}>
            {isPending ? <Trans>Updating...</Trans> : <Trans>Update payment method</Trans>}
          </Button>
        </DialogFooter>
      )}
    </>
  );
}
