import { i18n } from "@lingui/core";
import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { requirePermission, requireSubscriptionEnabled } from "@repo/infrastructure/auth/routeGuards";
import { AppLayout } from "@repo/ui/components/AppLayout";
import { Button } from "@repo/ui/components/Button";
import { Separator } from "@repo/ui/components/Separator";
import { useFormatLongDate } from "@repo/ui/hooks/useSmartDate";
import { loadStripe } from "@stripe/stripe-js";
import { createFileRoute } from "@tanstack/react-router";
import { AlertTriangleIcon } from "lucide-react";
import { useState } from "react";
import { toast } from "sonner";
import { api, SubscriptionPlan } from "@/shared/lib/api/client";
import { getPlanLabel } from "@/shared/lib/api/subscriptionPlan";
import { CancelDowngradeDialog } from "../-components/CancelDowngradeDialog";
import { CancelSubscriptionDialog } from "../-components/CancelSubscriptionDialog";
import { CheckoutDialog } from "../-components/CheckoutDialog";
import { DowngradeConfirmationDialog } from "../-components/DowngradeConfirmationDialog";
import { EditBillingInfoDialog } from "../-components/EditBillingInfoDialog";
import { getCatalogUnitAmount, getFormattedPrice, PlanCard } from "../-components/PlanCard";
import { ReactivateConfirmationDialog } from "../-components/ReactivateConfirmationDialog";
import { SubscriptionTabNavigation } from "../-components/SubscriptionTabNavigation";
import { SubscribeConfirmationDialog } from "../-components/SubscribeConfirmationDialog";
import { UpgradeConfirmationDialog } from "../-components/UpgradeConfirmationDialog";
import { useSubscriptionPolling } from "../-components/useSubscriptionPolling";

export const Route = createFileRoute("/account/subscription/plans/")({
  staticData: { trackingTitle: "Subscription plans" },
  beforeLoad: () => {
    requireSubscriptionEnabled();
    requirePermission({ allowedRoles: ["Owner"] });
  },
  component: PlansPage
});

function PlansPage() {
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

  const { data: tenant } = api.useQuery("get", "/api/account/tenants/current");
  const { data: pricingCatalog } = api.useQuery("get", "/api/account/subscriptions/pricing-catalog");

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
            // biome-ignore lint/style/useNamingConvention: Stripe API uses snake_case
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
            // biome-ignore lint/style/useNamingConvention: Stripe API uses snake_case
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

  const formatLongDate = useFormatLongDate();
  const isStripeConfigured = (pricingCatalog?.plans?.length ?? 0) > 0;
  const currentPlan = subscription?.plan ?? SubscriptionPlan.Basis;
  const cancelAtPeriodEnd = subscription?.cancelAtPeriodEnd ?? false;
  const scheduledPlan = subscription?.scheduledPlan ?? null;
  const currentPeriodEnd = subscription?.currentPeriodEnd ?? null;
  const billingInfo = subscription?.billingInfo;
  const formattedPeriodEnd = formatLongDate(currentPeriodEnd);

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
      : reactivateMutation.isPending
        ? currentPlan
        : null;

  const handleSubscribe = (plan: SubscriptionPlan) => {
    if (subscription?.billingInfo && subscription?.paymentMethod) {
      setSubscribeTarget(plan);
      setIsSubscribeDialogOpen(true);
    } else {
      setPendingCheckoutPlan(plan);
      setIsEditBillingInfoOpen(true);
    }
  };

  const handleUpgrade = (plan: SubscriptionPlan) => {
    setUpgradeTarget(plan);
    setIsUpgradeDialogOpen(true);
  };

  const handleDowngrade = (plan: SubscriptionPlan) => {
    setDowngradeTarget(plan);
    setIsDowngradeDialogOpen(true);
  };

  const handleConfirmUpgrade = () => {
    upgradeMutation.mutate({ body: { newPlan: upgradeTarget } });
  };

  const handleConfirmSubscribe = () => {
    subscribeMutation.mutate({ body: { plan: subscribeTarget } });
  };

  const handleConfirmDowngrade = () => {
    downgradeMutation.mutate({ body: { newPlan: downgradeTarget } });
  };

  const handleCancelDowngrade = () => {
    setIsCancelDowngradeDialogOpen(true);
  };

  const handleConfirmCancelDowngrade = () => {
    cancelDowngradeMutation.mutate({});
  };

  const handleReactivate = () => {
    setIsReactivateDialogOpen(true);
  };

  const handleConfirmReactivate = () => {
    reactivateMutation.mutate({});
  };

  const handleBillingInfoSuccess = () => {
    if (pendingCheckoutPlan == null) {
      return;
    }
    setCheckoutPlan(pendingCheckoutPlan);
    setPendingCheckoutPlan(null);
    setIsCheckoutDialogOpen(true);
  };

  return (
    <>
      <AppLayout
        variant="center"
        maxWidth="60rem"
        title={t`Subscription plans`}
        subtitle={t`Manage your subscription and billing.`}
      >
        <SubscriptionTabNavigation activeTab="plans" />

        {cancelAtPeriodEnd && (
          <div className="mb-6 flex items-center gap-3 rounded-lg border border-border bg-muted/50 p-4 text-muted-foreground text-sm">
            <AlertTriangleIcon className="size-4 shrink-0" />
            {formattedPeriodEnd ? (
              <Trans>
                Your {getPlanLabel(currentPlan)} subscription has been cancelled and will end on {formattedPeriodEnd}.
              </Trans>
            ) : (
              <Trans>Your subscription has been cancelled and will end at the end of the current billing period.</Trans>
            )}
          </div>
        )}

        {scheduledPlan && !cancelAtPeriodEnd && (
          <div className="mb-6 flex items-center gap-3 rounded-lg border border-border bg-muted/50 p-4 text-muted-foreground text-sm">
            <AlertTriangleIcon className="size-4 shrink-0" />
            {formattedPeriodEnd ? (
              <Trans>
                Your subscription will be downgraded to {getPlanLabel(scheduledPlan)} on {formattedPeriodEnd}.
              </Trans>
            ) : (
              <Trans>
                Your subscription will be downgraded to {getPlanLabel(scheduledPlan)} at the end of the current billing
                period.
              </Trans>
            )}
          </div>
        )}

        {!isStripeConfigured && (
          <div className="mb-6 flex items-center gap-3 rounded-lg border border-border bg-muted/50 p-4 text-muted-foreground text-sm">
            <AlertTriangleIcon className="size-4 shrink-0" />
            <Trans>Billing is not configured. Please contact support to enable payment processing.</Trans>
          </div>
        )}

        <div className="grid gap-4 lg:grid-cols-3">
          {[SubscriptionPlan.Basis, SubscriptionPlan.Standard, SubscriptionPlan.Premium].map((plan) => (
            <PlanCard
              key={plan}
              plan={plan}
              formattedPrice={getFormattedPrice(plan, pricingCatalog?.plans)}
              currentPlan={currentPlan}
              cancelAtPeriodEnd={cancelAtPeriodEnd}
              scheduledPlan={scheduledPlan}
              isStripeConfigured={isStripeConfigured}
              onSubscribe={handleSubscribe}
              onUpgrade={handleUpgrade}
              onDowngrade={handleDowngrade}
              onReactivate={handleReactivate}
              onCancelDowngrade={handleCancelDowngrade}
              isPending={isPending}
              pendingPlan={pendingPlan}
              isCancelDowngradePending={cancelDowngradeMutation.isPending}
              currentPriceAmount={subscription?.currentPriceAmount}
              currentPriceCurrency={subscription?.currentPriceCurrency}
              catalogUnitAmount={getCatalogUnitAmount(plan, pricingCatalog?.plans)}
            />
          ))}
        </div>

        {subscription?.hasStripeSubscription && !cancelAtPeriodEnd && (
          <div className="mt-8 flex flex-col gap-4">
            <h3>
              <Trans>Cancel subscription</Trans>
            </h3>
            <Separator />
            <p className="text-muted-foreground text-sm">
              {formattedPeriodEnd ? (
                <Trans>
                  If you cancel, you will keep access to your {getPlanLabel(currentPlan)} plan until{" "}
                  {formattedPeriodEnd}. After that, your account will be downgraded.
                </Trans>
              ) : (
                <Trans>
                  If you cancel, you will keep access to your current plan until the end of your billing period. After
                  that, your account will be downgraded.
                </Trans>
              )}
            </p>
            <Button
              variant="destructive"
              className="mt-2 w-fit max-sm:w-full"
              onClick={() => setIsCancelDialogOpen(true)}
              disabled={isPending}
            >
              <Trans>Cancel subscription</Trans>
            </Button>
          </div>
        )}
      </AppLayout>

      <CancelSubscriptionDialog
        isOpen={isCancelDialogOpen}
        onOpenChange={setIsCancelDialogOpen}
        onConfirm={(reason, feedback) => cancelMutation.mutate({ body: { reason, feedback } })}
        isPending={cancelMutation.isPending || isPolling}
        currentPeriodEnd={currentPeriodEnd}
      />

      <UpgradeConfirmationDialog
        isOpen={isUpgradeDialogOpen}
        onOpenChange={setIsUpgradeDialogOpen}
        onConfirm={handleConfirmUpgrade}
        isPending={upgradeMutation.isPending || isConfirmingPayment || isPolling}
        targetPlan={upgradeTarget}
        billingInfo={billingInfo}
        paymentMethod={subscription?.paymentMethod}
      />

      <SubscribeConfirmationDialog
        isOpen={isSubscribeDialogOpen}
        onOpenChange={setIsSubscribeDialogOpen}
        onConfirm={handleConfirmSubscribe}
        isPending={subscribeMutation.isPending || isConfirmingPayment || isPolling}
        targetPlan={subscribeTarget}
        billingInfo={billingInfo}
        paymentMethod={subscription?.paymentMethod}
      />

      <DowngradeConfirmationDialog
        isOpen={isDowngradeDialogOpen}
        onOpenChange={setIsDowngradeDialogOpen}
        onConfirm={handleConfirmDowngrade}
        isPending={downgradeMutation.isPending || isPolling}
        targetPlan={downgradeTarget}
        currentPeriodEnd={currentPeriodEnd}
      />

      {scheduledPlan && (
        <CancelDowngradeDialog
          isOpen={isCancelDowngradeDialogOpen}
          onOpenChange={setIsCancelDowngradeDialogOpen}
          onConfirm={handleConfirmCancelDowngrade}
          isPending={cancelDowngradeMutation.isPending || isPolling}
          currentPlan={currentPlan}
          scheduledPlan={scheduledPlan}
          currentPeriodEnd={currentPeriodEnd}
        />
      )}

      <ReactivateConfirmationDialog
        isOpen={isReactivateDialogOpen}
        onOpenChange={setIsReactivateDialogOpen}
        onConfirm={handleConfirmReactivate}
        isPending={reactivateMutation.isPending || isPolling}
        currentPlan={currentPlan}
      />

      <EditBillingInfoDialog
        isOpen={isEditBillingInfoOpen}
        onOpenChange={setIsEditBillingInfoOpen}
        billingInfo={billingInfo}
        tenantName={tenant?.name ?? ""}
        onSuccess={handleBillingInfoSuccess}
        submitLabel={t`Next`}
        pendingLabel={t`Saving...`}
      />

      <CheckoutDialog
        isOpen={isCheckoutDialogOpen}
        onOpenChange={(open) => {
          setIsCheckoutDialogOpen(open);
          if (!open) {
            setReactivateClientSecret(undefined);
            setReactivatePublishableKey(undefined);
          }
        }}
        plan={checkoutPlan}
        prefetchedClientSecret={reactivateClientSecret}
        prefetchedPublishableKey={reactivatePublishableKey}
      />
    </>
  );
}
