import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { requirePermission, requireSubscriptionEnabled } from "@repo/infrastructure/auth/routeGuards";
import { AppLayout } from "@repo/ui/components/AppLayout";
import { Separator } from "@repo/ui/components/Separator";
import { Skeleton } from "@repo/ui/components/Skeleton";
import { useFormatLongDate } from "@repo/ui/hooks/useSmartDate";
import { createFileRoute } from "@tanstack/react-router";
import { useState } from "react";

import { api, SubscriptionPlan } from "@/shared/lib/api/client";

import { BillingHistoryTable } from "./-components/BillingHistoryTable";
import { BillingInfoSection } from "./-components/BillingInfoSection";
import { BillingPageDialogs } from "./-components/BillingPageDialogs";
import { BillingTabNavigation } from "./-components/BillingTabNavigation";
import { CurrentPlanSection } from "./-components/CurrentPlanSection";
import { InitialPlanSelection } from "./-components/InitialPlanSelection";
import { PaymentMethodSection } from "./-components/PaymentMethodSection";
import { CancellationBanner, DowngradeBanner } from "./-components/SubscriptionBanner";
import { useBillingPageMutations } from "./-components/useBillingPageMutations";
import { useSubscriptionPolling } from "./-components/useSubscriptionPolling";

export const Route = createFileRoute("/account/billing/")({
  staticData: { trackingTitle: "Billing" },
  beforeLoad: () => {
    requireSubscriptionEnabled();
    requirePermission({ allowedRoles: ["Owner"] });
  },
  component: BillingPage
});

function BillingPage() {
  const formatLongDate = useFormatLongDate();
  const { isPolling, isLoading, startPolling, subscription } = useSubscriptionPolling();
  const [isCancelDowngradeDialogOpen, setIsCancelDowngradeDialogOpen] = useState(false);
  const [isReactivateDialogOpen, setIsReactivateDialogOpen] = useState(false);
  const [isEditBillingInfoOpen, setIsEditBillingInfoOpen] = useState(false);
  const [isUpdatePaymentMethodOpen, setIsUpdatePaymentMethodOpen] = useState(false);
  const [isRetryPaymentOpen, setIsRetryPaymentOpen] = useState(false);
  const [retryInvoice, setRetryInvoice] = useState({ amount: 0, currency: "" });
  const [isCheckoutDialogOpen, setIsCheckoutDialogOpen] = useState(false);
  const [checkoutPlan, setCheckoutPlan] = useState<SubscriptionPlan>(SubscriptionPlan.Basis);
  const [pendingCheckoutPlan, setPendingCheckoutPlan] = useState<SubscriptionPlan | null>(null);
  const [reactivateClientSecret, setReactivateClientSecret] = useState<string | undefined>();
  const [reactivatePublishableKey, setReactivatePublishableKey] = useState<string | undefined>();

  const { data: tenant } = api.useQuery("get", "/api/account/tenants/current");
  const { data: pricingCatalog } = api.useQuery("get", "/api/account/subscriptions/pricing-catalog");
  const currentPlan = subscription?.plan ?? SubscriptionPlan.Basis;

  const { reactivateMutation, cancelDowngradeMutation } = useBillingPageMutations({
    startPolling,
    currentPlan,
    setIsReactivateDialogOpen,
    setReactivateClientSecret,
    setReactivatePublishableKey,
    setPendingCheckoutPlan,
    setIsEditBillingInfoOpen,
    setIsCancelDowngradeDialogOpen
  });

  const isStripeConfigured = (pricingCatalog?.plans?.length ?? 0) > 0;
  const cancelAtPeriodEnd = subscription?.cancelAtPeriodEnd ?? false;
  const scheduledPlan = subscription?.scheduledPlan ?? null;
  const currentPeriodEnd = subscription?.currentPeriodEnd ?? null;
  const hasStripeCustomer = subscription?.hasStripeCustomer ?? false;
  const formattedPeriodEndLong = formatLongDate(currentPeriodEnd);

  const handleBillingInfoSuccess = () => {
    if (pendingCheckoutPlan == null) return;
    setCheckoutPlan(pendingCheckoutPlan);
    setPendingCheckoutPlan(null);
    setIsCheckoutDialogOpen(true);
  };

  if (isLoading) {
    return (
      <AppLayout variant="center" maxWidth="64rem" title={t`Billing`}>
        <Skeleton className="h-6 w-48" />
      </AppLayout>
    );
  }

  return (
    <>
      {hasStripeCustomer ? (
        <AppLayout
          variant="center"
          maxWidth="64rem"
          title={t`Billing`}
          subtitle={t`Manage your payment methods and billing information.`}
        >
          <BillingTabNavigation activeTab="billing" />
          {cancelAtPeriodEnd && (
            <CancellationBanner
              currentPlan={currentPlan}
              formattedPeriodEnd={formattedPeriodEndLong}
              onReactivate={() => setIsReactivateDialogOpen(true)}
            />
          )}
          {scheduledPlan && !cancelAtPeriodEnd && (
            <DowngradeBanner
              scheduledPlan={scheduledPlan}
              formattedPeriodEnd={formattedPeriodEndLong}
              onCancelDowngrade={() => setIsCancelDowngradeDialogOpen(true)}
            />
          )}
          <CurrentPlanSection
            currentPlan={currentPlan}
            cancelAtPeriodEnd={cancelAtPeriodEnd}
            scheduledPlan={scheduledPlan}
            formattedPeriodEndLong={formattedPeriodEndLong}
            currentPriceAmount={subscription?.currentPriceAmount}
            currentPriceCurrency={subscription?.currentPriceCurrency}
            plans={pricingCatalog?.plans}
          />
          <PaymentMethodSection
            paymentMethod={subscription?.paymentMethod}
            isStripeConfigured={isStripeConfigured}
            onUpdateClick={() => setIsUpdatePaymentMethodOpen(true)}
          />
          <BillingInfoSection
            billingInfo={subscription?.billingInfo}
            isStripeConfigured={isStripeConfigured}
            onEditClick={() => setIsEditBillingInfoOpen(true)}
          />
          <div className="mt-8 flex flex-col gap-4">
            <h3>
              <Trans>Billing history</Trans>
            </h3>
            <Separator />
            <BillingHistoryTable />
          </div>
        </AppLayout>
      ) : (
        <InitialPlanSelection
          plans={pricingCatalog?.plans}
          currentPlan={currentPlan}
          isStripeConfigured={isStripeConfigured}
          onSubscribe={(plan) => {
            setPendingCheckoutPlan(plan);
            setIsEditBillingInfoOpen(true);
          }}
        />
      )}
      <BillingPageDialogs
        scheduledPlan={scheduledPlan}
        isCancelDowngradeDialogOpen={isCancelDowngradeDialogOpen}
        setIsCancelDowngradeDialogOpen={setIsCancelDowngradeDialogOpen}
        onCancelDowngradeConfirm={() => cancelDowngradeMutation.mutate({})}
        isCancelDowngradePending={cancelDowngradeMutation.isPending || isPolling}
        currentPlan={currentPlan}
        currentPeriodEnd={currentPeriodEnd}
        isReactivateDialogOpen={isReactivateDialogOpen}
        setIsReactivateDialogOpen={setIsReactivateDialogOpen}
        onReactivateConfirm={() => reactivateMutation.mutate({})}
        isReactivatePending={reactivateMutation.isPending || isPolling}
        isEditBillingInfoOpen={isEditBillingInfoOpen}
        setIsEditBillingInfoOpen={setIsEditBillingInfoOpen}
        billingInfo={subscription?.billingInfo}
        tenantName={tenant?.name ?? ""}
        onBillingInfoSuccess={handleBillingInfoSuccess}
        pendingCheckoutPlan={pendingCheckoutPlan}
        isUpdatePaymentMethodOpen={isUpdatePaymentMethodOpen}
        setIsUpdatePaymentMethodOpen={setIsUpdatePaymentMethodOpen}
        onHasOpenInvoice={(invoice) => {
          setRetryInvoice(invoice);
          setIsRetryPaymentOpen(true);
        }}
        isRetryPaymentOpen={isRetryPaymentOpen}
        setIsRetryPaymentOpen={setIsRetryPaymentOpen}
        paymentMethod={subscription?.paymentMethod}
        retryInvoiceAmount={retryInvoice.amount}
        retryInvoiceCurrency={retryInvoice.currency}
        isCheckoutDialogOpen={isCheckoutDialogOpen}
        setIsCheckoutDialogOpen={setIsCheckoutDialogOpen}
        checkoutPlan={checkoutPlan}
        reactivateClientSecret={reactivateClientSecret}
        reactivatePublishableKey={reactivatePublishableKey}
        setReactivateClientSecret={setReactivateClientSecret}
        setReactivatePublishableKey={setReactivatePublishableKey}
      />
    </>
  );
}
