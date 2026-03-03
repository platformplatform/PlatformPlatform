import { t } from "@lingui/core/macro";
import { requirePermission, requireSubscriptionEnabled } from "@repo/infrastructure/auth/routeGuards";
import { AppLayout } from "@repo/ui/components/AppLayout";
import { useFormatLongDate } from "@repo/ui/hooks/useSmartDate";
import { createFileRoute } from "@tanstack/react-router";

import { SubscriptionPlan } from "@/shared/lib/api/client";

import { BillingTabNavigation } from "../-components/BillingTabNavigation";
import { PlanCardGrid } from "../-components/PlanCardGrid";
import { CancellationBanner, DowngradeBanner, StripeNotConfiguredBanner } from "../-components/SubscriptionBanner";
import { SubscriptionDialogs } from "../-components/SubscriptionDialogs";
import { usePlansPageState } from "../-components/usePlansPageState";

export const Route = createFileRoute("/account/billing/subscription/")({
  staticData: { trackingTitle: "Subscription" },
  beforeLoad: () => {
    requireSubscriptionEnabled();
    requirePermission({ allowedRoles: ["Owner"] });
  },
  component: PlansPage
});

function PlansPage() {
  const state = usePlansPageState();
  const formatLongDate = useFormatLongDate();

  const cancelAtPeriodEnd = state.subscription?.cancelAtPeriodEnd ?? false;
  const scheduledPlan = (state.subscription?.scheduledPlan ?? null) as SubscriptionPlan | null;
  const currentPeriodEnd = state.subscription?.currentPeriodEnd ?? null;
  const formattedPeriodEnd = formatLongDate(currentPeriodEnd);
  const isStripeConfigured = (state.pricingCatalog?.plans?.length ?? 0) > 0;

  const handleSubscribe = (plan: SubscriptionPlan) => {
    if (state.subscription?.billingInfo && state.subscription?.paymentMethod) {
      state.setSubscribeTarget(plan);
      state.setIsSubscribeDialogOpen(true);
    } else {
      state.setPendingCheckoutPlan(plan);
      state.setIsEditBillingInfoOpen(true);
    }
  };

  const handleDowngrade = (plan: SubscriptionPlan) => {
    if (plan === SubscriptionPlan.Basis) {
      state.setIsCancelDialogOpen(true);
    } else {
      state.setDowngradeTarget(plan);
      state.setIsDowngradeDialogOpen(true);
    }
  };

  return (
    <>
      <AppLayout variant="center" maxWidth="64rem" title={t`Subscription`} subtitle={t`Manage your subscription plan.`}>
        <BillingTabNavigation activeTab="subscription" />
        {cancelAtPeriodEnd && (
          <CancellationBanner currentPlan={state.currentPlan} formattedPeriodEnd={formattedPeriodEnd} />
        )}
        {scheduledPlan && !cancelAtPeriodEnd && (
          <DowngradeBanner scheduledPlan={scheduledPlan} formattedPeriodEnd={formattedPeriodEnd} />
        )}
        {!isStripeConfigured && <StripeNotConfiguredBanner />}
        <PlanCardGrid
          plans={state.pricingCatalog?.plans}
          currentPlan={state.currentPlan}
          cancelAtPeriodEnd={cancelAtPeriodEnd}
          scheduledPlan={scheduledPlan}
          isStripeConfigured={isStripeConfigured}
          onSubscribe={handleSubscribe}
          onUpgrade={(plan) => {
            state.setUpgradeTarget(plan);
            state.setIsUpgradeDialogOpen(true);
          }}
          onDowngrade={handleDowngrade}
          onReactivate={() => state.setIsReactivateDialogOpen(true)}
          onCancelDowngrade={() => state.setIsCancelDowngradeDialogOpen(true)}
          isPending={state.isPending}
          pendingPlan={state.pendingPlan}
          isCancelDowngradePending={state.cancelDowngradeMutation.isPending}
          currentPriceAmount={state.subscription?.currentPriceAmount}
          currentPriceCurrency={state.subscription?.currentPriceCurrency}
        />
      </AppLayout>
      <SubscriptionDialogs
        isCancelDialogOpen={state.isCancelDialogOpen}
        setIsCancelDialogOpen={state.setIsCancelDialogOpen}
        onCancelConfirm={(reason, feedback) => state.cancelMutation.mutate({ body: { reason, feedback } })}
        isCancelPending={state.cancelMutation.isPending || state.isPolling}
        currentPeriodEnd={currentPeriodEnd}
        isUpgradeDialogOpen={state.isUpgradeDialogOpen}
        setIsUpgradeDialogOpen={state.setIsUpgradeDialogOpen}
        onUpgradeConfirm={() => state.upgradeMutation.mutate({ body: { newPlan: state.upgradeTarget } })}
        isUpgradePending={state.upgradeMutation.isPending || state.isConfirmingPayment || state.isPolling}
        upgradeTarget={state.upgradeTarget}
        isSubscribeDialogOpen={state.isSubscribeDialogOpen}
        setIsSubscribeDialogOpen={state.setIsSubscribeDialogOpen}
        onSubscribeConfirm={() => state.subscribeMutation.mutate({ body: { plan: state.subscribeTarget } })}
        isSubscribePending={state.subscribeMutation.isPending || state.isConfirmingPayment || state.isPolling}
        subscribeTarget={state.subscribeTarget}
        isDowngradeDialogOpen={state.isDowngradeDialogOpen}
        setIsDowngradeDialogOpen={state.setIsDowngradeDialogOpen}
        onDowngradeConfirm={() => state.downgradeMutation.mutate({ body: { newPlan: state.downgradeTarget } })}
        isDowngradePending={state.downgradeMutation.isPending || state.isPolling}
        downgradeTarget={state.downgradeTarget}
        scheduledPlan={scheduledPlan}
        isCancelDowngradeDialogOpen={state.isCancelDowngradeDialogOpen}
        setIsCancelDowngradeDialogOpen={state.setIsCancelDowngradeDialogOpen}
        onCancelDowngradeConfirm={() => state.cancelDowngradeMutation.mutate({})}
        isCancelDowngradePending={state.cancelDowngradeMutation.isPending || state.isPolling}
        currentPlan={state.currentPlan}
        isReactivateDialogOpen={state.isReactivateDialogOpen}
        setIsReactivateDialogOpen={state.setIsReactivateDialogOpen}
        onReactivateConfirm={() => state.reactivateMutation.mutate({})}
        isReactivatePending={state.reactivateMutation.isPending || state.isPolling}
        isEditBillingInfoOpen={state.isEditBillingInfoOpen}
        setIsEditBillingInfoOpen={state.setIsEditBillingInfoOpen}
        billingInfo={state.subscription?.billingInfo}
        paymentMethod={state.subscription?.paymentMethod}
        tenantName={state.tenant?.name ?? ""}
        onBillingInfoSuccess={state.handleBillingInfoSuccess}
        isCheckoutDialogOpen={state.isCheckoutDialogOpen}
        setIsCheckoutDialogOpen={state.setIsCheckoutDialogOpen}
        checkoutPlan={state.checkoutPlan}
        reactivateClientSecret={state.reactivateClientSecret}
        reactivatePublishableKey={state.reactivatePublishableKey}
        setReactivateClientSecret={state.setReactivateClientSecret}
        setReactivatePublishableKey={state.setReactivatePublishableKey}
      />
    </>
  );
}
