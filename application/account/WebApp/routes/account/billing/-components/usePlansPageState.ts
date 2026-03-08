import { useTenant } from "@repo/infrastructure/sync/hooks";
import { useState } from "react";

import { api, SubscriptionPlan } from "@/shared/lib/api/client";

import { useSubscriptionLifecycleMutations } from "./useSubscriptionMutations";
import { useSubscriptionPolling } from "./useSubscriptionPolling";
import { useUpgradeSubscribeMutations } from "./useUpgradeSubscribeMutations";

export function usePlansPageState() {
  const { isPolling, startPolling, subscription } = useSubscriptionPolling();
  const [isUpgradeDialogOpen, setIsUpgradeDialogOpen] = useState(false);
  const [upgradeTarget, setUpgradeTarget] = useState<SubscriptionPlan>(SubscriptionPlan.Standard);
  const [isDowngradeDialogOpen, setIsDowngradeDialogOpen] = useState(false);
  const [downgradeTarget, setDowngradeTarget] = useState<SubscriptionPlan>(SubscriptionPlan.Standard);
  const [isCancelDialogOpen, setIsCancelDialogOpen] = useState(false);
  const [isCancelDowngradeDialogOpen, setIsCancelDowngradeDialogOpen] = useState(false);
  const [isReactivateDialogOpen, setIsReactivateDialogOpen] = useState(false);
  const [isCheckoutDialogOpen, setIsCheckoutDialogOpen] = useState(false);
  const [isEditBillingInfoOpen, setIsEditBillingInfoOpen] = useState(false);
  const [checkoutPlan, setCheckoutPlan] = useState<SubscriptionPlan>(SubscriptionPlan.Standard);
  const [pendingCheckoutPlan, setPendingCheckoutPlan] = useState<SubscriptionPlan | null>(null);
  const [reactivateClientSecret, setReactivateClientSecret] = useState<string | undefined>();
  const [reactivatePublishableKey, setReactivatePublishableKey] = useState<string | undefined>();
  const [isConfirmingPayment, setIsConfirmingPayment] = useState(false);
  const [isSubscribeDialogOpen, setIsSubscribeDialogOpen] = useState(false);
  const [subscribeTarget, setSubscribeTarget] = useState<SubscriptionPlan>(SubscriptionPlan.Standard);

  const { tenantId } = import.meta.user_info_env;
  const { data: tenant } = useTenant(tenantId ?? "");
  const { data: pricingCatalog } = api.useQuery("get", "/api/account/subscriptions/pricing-catalog");
  const currentPlan = (subscription?.plan ?? SubscriptionPlan.Basis) as SubscriptionPlan;

  const { upgradeMutation, subscribeMutation } = useUpgradeSubscribeMutations({
    startPolling,
    setIsUpgradeDialogOpen,
    setIsSubscribeDialogOpen,
    setIsConfirmingPayment
  });

  const { downgradeMutation, cancelMutation, cancelDowngradeMutation, reactivateMutation } =
    useSubscriptionLifecycleMutations({
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
    });

  const isPending =
    upgradeMutation.isPending ||
    subscribeMutation.isPending ||
    downgradeMutation.isPending ||
    cancelDowngradeMutation.isPending ||
    reactivateMutation.isPending ||
    cancelMutation.isPending ||
    isPolling;

  const pendingPlan = upgradeMutation.isPending
    ? (upgradeMutation.variables?.body?.newPlan ?? null)
    : downgradeMutation.isPending
      ? downgradeTarget
      : cancelMutation.isPending
        ? SubscriptionPlan.Basis
        : reactivateMutation.isPending
          ? currentPlan
          : null;

  const handleBillingInfoSuccess = () => {
    if (pendingCheckoutPlan == null) return;
    setCheckoutPlan(pendingCheckoutPlan);
    setPendingCheckoutPlan(null);
    setIsCheckoutDialogOpen(true);
  };

  return {
    subscription,
    isPolling,
    isConfirmingPayment,
    tenant,
    pricingCatalog,
    currentPlan,
    isPending,
    pendingPlan,
    isUpgradeDialogOpen,
    setIsUpgradeDialogOpen,
    upgradeTarget,
    setUpgradeTarget,
    isDowngradeDialogOpen,
    setIsDowngradeDialogOpen,
    downgradeTarget,
    setDowngradeTarget,
    isCancelDialogOpen,
    setIsCancelDialogOpen,
    isCancelDowngradeDialogOpen,
    setIsCancelDowngradeDialogOpen,
    isReactivateDialogOpen,
    setIsReactivateDialogOpen,
    isCheckoutDialogOpen,
    setIsCheckoutDialogOpen,
    isEditBillingInfoOpen,
    setIsEditBillingInfoOpen,
    checkoutPlan,
    pendingCheckoutPlan,
    setPendingCheckoutPlan,
    reactivateClientSecret,
    reactivatePublishableKey,
    setReactivateClientSecret,
    setReactivatePublishableKey,
    isSubscribeDialogOpen,
    setIsSubscribeDialogOpen,
    subscribeTarget,
    setSubscribeTarget,
    upgradeMutation,
    subscribeMutation,
    downgradeMutation,
    cancelMutation,
    cancelDowngradeMutation,
    reactivateMutation,
    handleBillingInfoSuccess
  };
}
