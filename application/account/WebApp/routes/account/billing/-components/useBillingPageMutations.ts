import { t } from "@lingui/core/macro";

import { api, type SubscriptionPlan } from "@/shared/lib/api/client";

import type { useSubscriptionPolling } from "./useSubscriptionPolling";

interface UseBillingPageMutationsOptions {
  startPolling: ReturnType<typeof useSubscriptionPolling>["startPolling"];
  currentPlan: SubscriptionPlan;
  setIsReactivateDialogOpen: (open: boolean) => void;
  setReactivateClientSecret: (value: string | undefined) => void;
  setReactivatePublishableKey: (value: string | undefined) => void;
  setPendingCheckoutPlan: (plan: SubscriptionPlan | null) => void;
  setIsEditBillingInfoOpen: (open: boolean) => void;
  setIsCancelDowngradeDialogOpen: (open: boolean) => void;
}

export function useBillingPageMutations({
  startPolling,
  currentPlan,
  setIsReactivateDialogOpen,
  setReactivateClientSecret,
  setReactivatePublishableKey,
  setPendingCheckoutPlan,
  setIsEditBillingInfoOpen,
  setIsCancelDowngradeDialogOpen
}: UseBillingPageMutationsOptions) {
  const reactivateMutation = api.useMutation("post", "/api/account/subscriptions/reactivate", {
    onSuccess: (data) => {
      if (data.clientSecret && data.publishableKey) {
        setIsReactivateDialogOpen(false);
        setReactivateClientSecret(data.clientSecret);
        setReactivatePublishableKey(data.publishableKey);
        setPendingCheckoutPlan(currentPlan);
        setIsEditBillingInfoOpen(true);
      } else {
        startPolling({
          check: (subscription) => subscription.cancelAtPeriodEnd === false,
          successMessage: t`Your subscription has been reactivated.`,
          onComplete: () => setIsReactivateDialogOpen(false)
        });
      }
    }
  });

  const cancelDowngradeMutation = api.useMutation("post", "/api/account/subscriptions/cancel-downgrade", {
    onSuccess: () => {
      startPolling({
        check: (subscription) => subscription.scheduledPlan == null,
        successMessage: t`Your scheduled downgrade has been cancelled.`,
        onComplete: () => setIsCancelDowngradeDialogOpen(false)
      });
    }
  });

  return { reactivateMutation, cancelDowngradeMutation };
}
