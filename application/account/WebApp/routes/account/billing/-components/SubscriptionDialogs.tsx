import type { BillingInfo, PaymentMethod } from "@repo/infrastructure/sync/hooks";

import { t } from "@lingui/core/macro";

import type { CancellationReason, SubscriptionPlan } from "@/shared/lib/api/client";

import { CancelDowngradeDialog } from "./CancelDowngradeDialog";
import { CancelSubscriptionDialog } from "./CancelSubscriptionDialog";
import { CheckoutDialog } from "./CheckoutDialog";
import { DowngradeConfirmationDialog } from "./DowngradeConfirmationDialog";
import { EditBillingInfoDialog } from "./EditBillingInfoDialog";
import { ReactivateConfirmationDialog } from "./ReactivateConfirmationDialog";
import { SubscribeConfirmationDialog } from "./SubscribeConfirmationDialog";
import { UpgradeConfirmationDialog } from "./UpgradeConfirmationDialog";

interface SubscriptionDialogsProps {
  isCancelDialogOpen: boolean;
  setIsCancelDialogOpen: (open: boolean) => void;
  onCancelConfirm: (reason: CancellationReason, feedback: string | null) => void;
  isCancelPending: boolean;
  currentPeriodEnd: string | null;

  isUpgradeDialogOpen: boolean;
  setIsUpgradeDialogOpen: (open: boolean) => void;
  onUpgradeConfirm: () => void;
  isUpgradePending: boolean;
  upgradeTarget: SubscriptionPlan;

  isSubscribeDialogOpen: boolean;
  setIsSubscribeDialogOpen: (open: boolean) => void;
  onSubscribeConfirm: () => void;
  isSubscribePending: boolean;
  subscribeTarget: SubscriptionPlan;

  isDowngradeDialogOpen: boolean;
  setIsDowngradeDialogOpen: (open: boolean) => void;
  onDowngradeConfirm: () => void;
  isDowngradePending: boolean;
  downgradeTarget: SubscriptionPlan;

  scheduledPlan: SubscriptionPlan | null;
  isCancelDowngradeDialogOpen: boolean;
  setIsCancelDowngradeDialogOpen: (open: boolean) => void;
  onCancelDowngradeConfirm: () => void;
  isCancelDowngradePending: boolean;
  currentPlan: SubscriptionPlan;

  isReactivateDialogOpen: boolean;
  setIsReactivateDialogOpen: (open: boolean) => void;
  onReactivateConfirm: () => void;
  isReactivatePending: boolean;

  isEditBillingInfoOpen: boolean;
  setIsEditBillingInfoOpen: (open: boolean) => void;
  billingInfo: BillingInfo | null | undefined;
  paymentMethod: PaymentMethod | null | undefined;
  tenantName: string;
  onBillingInfoSuccess: () => void;

  isCheckoutDialogOpen: boolean;
  setIsCheckoutDialogOpen: (open: boolean) => void;
  checkoutPlan: SubscriptionPlan;
  reactivateClientSecret: string | undefined;
  reactivatePublishableKey: string | undefined;
  setReactivateClientSecret: (value: string | undefined) => void;
  setReactivatePublishableKey: (value: string | undefined) => void;
}

export function SubscriptionDialogs({
  isCancelDialogOpen,
  setIsCancelDialogOpen,
  onCancelConfirm,
  isCancelPending,
  currentPeriodEnd,
  isUpgradeDialogOpen,
  setIsUpgradeDialogOpen,
  onUpgradeConfirm,
  isUpgradePending,
  upgradeTarget,
  isSubscribeDialogOpen,
  setIsSubscribeDialogOpen,
  onSubscribeConfirm,
  isSubscribePending,
  subscribeTarget,
  isDowngradeDialogOpen,
  setIsDowngradeDialogOpen,
  onDowngradeConfirm,
  isDowngradePending,
  downgradeTarget,
  scheduledPlan,
  isCancelDowngradeDialogOpen,
  setIsCancelDowngradeDialogOpen,
  onCancelDowngradeConfirm,
  isCancelDowngradePending,
  currentPlan,
  isReactivateDialogOpen,
  setIsReactivateDialogOpen,
  onReactivateConfirm,
  isReactivatePending,
  isEditBillingInfoOpen,
  setIsEditBillingInfoOpen,
  billingInfo,
  paymentMethod,
  tenantName,
  onBillingInfoSuccess,
  isCheckoutDialogOpen,
  setIsCheckoutDialogOpen,
  checkoutPlan,
  reactivateClientSecret,
  reactivatePublishableKey,
  setReactivateClientSecret,
  setReactivatePublishableKey
}: Readonly<SubscriptionDialogsProps>) {
  return (
    <>
      <CancelSubscriptionDialog
        isOpen={isCancelDialogOpen}
        onOpenChange={setIsCancelDialogOpen}
        onConfirm={onCancelConfirm}
        isPending={isCancelPending}
        currentPeriodEnd={currentPeriodEnd}
      />

      <UpgradeConfirmationDialog
        isOpen={isUpgradeDialogOpen}
        onOpenChange={setIsUpgradeDialogOpen}
        onConfirm={onUpgradeConfirm}
        isPending={isUpgradePending}
        targetPlan={upgradeTarget}
        billingInfo={billingInfo}
        paymentMethod={paymentMethod}
      />

      <SubscribeConfirmationDialog
        isOpen={isSubscribeDialogOpen}
        onOpenChange={setIsSubscribeDialogOpen}
        onConfirm={onSubscribeConfirm}
        isPending={isSubscribePending}
        targetPlan={subscribeTarget}
        billingInfo={billingInfo}
        paymentMethod={paymentMethod}
      />

      <DowngradeConfirmationDialog
        isOpen={isDowngradeDialogOpen}
        onOpenChange={setIsDowngradeDialogOpen}
        onConfirm={onDowngradeConfirm}
        isPending={isDowngradePending}
        targetPlan={downgradeTarget}
        currentPeriodEnd={currentPeriodEnd}
      />

      {scheduledPlan && (
        <CancelDowngradeDialog
          isOpen={isCancelDowngradeDialogOpen}
          onOpenChange={setIsCancelDowngradeDialogOpen}
          onConfirm={onCancelDowngradeConfirm}
          isPending={isCancelDowngradePending}
          currentPlan={currentPlan}
          scheduledPlan={scheduledPlan}
          currentPeriodEnd={currentPeriodEnd}
        />
      )}

      <ReactivateConfirmationDialog
        isOpen={isReactivateDialogOpen}
        onOpenChange={setIsReactivateDialogOpen}
        onConfirm={onReactivateConfirm}
        isPending={isReactivatePending}
        currentPlan={currentPlan}
      />

      <EditBillingInfoDialog
        isOpen={isEditBillingInfoOpen}
        onOpenChange={setIsEditBillingInfoOpen}
        billingInfo={billingInfo}
        tenantName={tenantName}
        onSuccess={onBillingInfoSuccess}
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
