import { i18n } from "@lingui/core";
import { t } from "@lingui/core/macro";
import { loadStripe } from "@stripe/stripe-js/pure";
import { toast } from "sonner";

import { api } from "@/shared/lib/api/client";

import type { useSubscriptionPolling } from "./useSubscriptionPolling";

interface UseUpgradeSubscribeMutationsOptions {
  startPolling: ReturnType<typeof useSubscriptionPolling>["startPolling"];
  setIsUpgradeDialogOpen: (open: boolean) => void;
  setIsSubscribeDialogOpen: (open: boolean) => void;
  setIsConfirmingPayment: (value: boolean) => void;
}

export function useUpgradeSubscribeMutations({
  startPolling,
  setIsUpgradeDialogOpen,
  setIsSubscribeDialogOpen,
  setIsConfirmingPayment
}: UseUpgradeSubscribeMutationsOptions) {
  const upgradeMutation = api.useMutation("post", "/api/account/subscriptions/upgrade", {
    onSuccess: async (data, variables) => {
      const targetPlan = variables.body?.newPlan;
      if (data.clientSecret && data.publishableKey) {
        setIsConfirmingPayment(true);
        const stripe = await loadStripe(data.publishableKey, { locale: i18n.locale as "auto" });
        if (!stripe) {
          setIsConfirmingPayment(false);
          toast.error(t`Failed to load payment processor.`);
          return;
        }
        const result = await stripe.confirmPayment({
          clientSecret: data.clientSecret,
          confirmParams: {
            return_url: window.location.href
          },
          redirect: "if_required"
        });
        setIsConfirmingPayment(false);
        if (result.error) {
          toast.error(result.error.message ?? t`Payment authentication failed.`);
          return;
        }
        startPolling({
          check: (subscription) => subscription.plan === targetPlan,
          successMessage: t`Your plan has been upgraded.`,
          onComplete: () => setIsUpgradeDialogOpen(false)
        });
      } else {
        startPolling({
          check: (subscription) => subscription.plan === targetPlan,
          successMessage: t`Your plan has been upgraded.`,
          onComplete: () => setIsUpgradeDialogOpen(false)
        });
      }
    }
  });

  const subscribeMutation = api.useMutation("post", "/api/account/subscriptions/start-checkout", {
    onSuccess: async (data, variables) => {
      const targetPlan = variables.body?.plan;
      if (data.clientSecret && data.publishableKey) {
        setIsConfirmingPayment(true);
        const stripe = await loadStripe(data.publishableKey, { locale: i18n.locale as "auto" });
        if (!stripe) {
          setIsConfirmingPayment(false);
          toast.error(t`Failed to load payment processor.`);
          return;
        }
        const result = await stripe.confirmPayment({
          clientSecret: data.clientSecret,
          confirmParams: {
            return_url: window.location.href
          },
          redirect: "if_required"
        });
        setIsConfirmingPayment(false);
        if (result.error) {
          toast.error(result.error.message ?? t`Payment authentication failed.`);
          return;
        }
        startPolling({
          check: (sub) => sub.plan === targetPlan,
          successMessage: t`Your subscription has been activated.`,
          onComplete: () => setIsSubscribeDialogOpen(false)
        });
      } else {
        startPolling({
          check: (sub) => sub.plan === targetPlan,
          successMessage: t`Your subscription has been activated.`,
          onComplete: () => setIsSubscribeDialogOpen(false)
        });
      }
    }
  });

  return { upgradeMutation, subscribeMutation };
}
