import { t } from "@lingui/core/macro";

import { api, type SubscriptionPlan } from "@/shared/lib/api/client";

import type { useSubscriptionPolling } from "./useSubscriptionPolling";

interface UseSubscriptionMutationsOptions {
  startPolling: ReturnType<typeof useSubscriptionPolling>["startPolling"];
  currentPlan: SubscriptionPlan;
  downgradeTarget: SubscriptionPlan;
  setIsDowngradeDialogOpen: (open: boolean) => void;
  setIsCancelDialogOpen: (open: boolean) => void;
  setIsCancelDowngradeDialogOpen: (open: boolean) => void;
  setIsReactivateDialogOpen: (open: boolean) => void;
  setIsEditBillingInfoOpen: (open: boolean) => void;
  setReactivateClientSecret: (value: string | undefined) => void;
  setReactivatePublishableKey: (value: string | undefined) => void;
  setPendingCheckoutPlan: (plan: SubscriptionPlan | null) => void;
}

export function useSubscriptionLifecycleMutations({
  startPolling,
  currentPlan,
  downgradeTarget,
  setIsDowngradeDialogOpen,
  setIsCancelDialogOpen,
  setIsCancelDowngradeDialogOpen,
  setIsReactivateDialogOpen,
  setIsEditBillingInfoOpen,
  setReactivateClientSecret,
  setReactivatePublishableKey,
  setPendingCheckoutPlan
}: UseSubscriptionMutationsOptions) {
  const downgradeMutation = api.useMutation("post", "/api/account/subscriptions/schedule-downgrade", {
    onSuccess: () => {
      startPolling({
        check: (subscription) => subscription.scheduledPlan === downgradeTarget,
        successMessage: t`Your downgrade has been scheduled.`,
        onComplete: () => setIsDowngradeDialogOpen(false)
      });
    }
  });

  const cancelMutation = api.useMutation("post", "/api/account/subscriptions/cancel", {
    onSuccess: () => {
      startPolling({
        check: (subscription) => subscription.cancelAtPeriodEnd === true,
        successMessage: t`Your subscription has been cancelled.`,
        onComplete: () => setIsCancelDialogOpen(false)
      });
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

  return { downgradeMutation, cancelMutation, cancelDowngradeMutation, reactivateMutation };
}
