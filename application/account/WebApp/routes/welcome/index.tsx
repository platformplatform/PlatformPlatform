import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { loggedInPath } from "@repo/infrastructure/auth/constants";
import { tenantCollection, userCollection } from "@repo/infrastructure/sync/collections";
import { useTenant, useUser } from "@repo/infrastructure/sync/hooks";
import { useElectricMutation } from "@repo/infrastructure/sync/useElectricMutation";
import { Button } from "@repo/ui/components/Button";
import { Form } from "@repo/ui/components/Form";
import { Link } from "@repo/ui/components/Link";
import { Skeleton } from "@repo/ui/components/Skeleton";
import { mutationSubmitter } from "@repo/ui/forms/mutationSubmitter";
import { createFileRoute } from "@tanstack/react-router";
import { useState } from "react";
import ErrorPage from "@/federated-modules/errorPages/ErrorPage";
import { AccountFields } from "@/shared/components/AccountFields";
import { UserProfileFields } from "@/shared/components/UserProfileFields";
import logoMarkUrl from "@/shared/images/logo-mark.svg";
import { HorizontalHeroLayout } from "@/shared/layouts/HorizontalHeroLayout";
import { apiClient } from "@/shared/lib/api/client";

export const Route = createFileRoute("/welcome/")({
  staticData: { trackingTitle: "Welcome" },
  component: WelcomePage,
  errorComponent: ErrorPage
});

type WelcomeStep = "account" | "profile";

function WelcomePage() {
  const { isAuthenticated, firstName, role, tenantName } = import.meta.user_info_env;
  const isOwner = role === "Owner";

  // Determine initial step based on what's completed
  const hasCompletedAccountSetup = !isOwner || !!tenantName;
  const hasCompletedProfileSetup = !!firstName;
  const initialStep: WelcomeStep = hasCompletedAccountSetup ? "profile" : "account";

  const [step, setStep] = useState<WelcomeStep>(initialStep);

  // If not authenticated, redirect to login
  if (!isAuthenticated) {
    window.location.href = "/login";
    return null;
  }

  // If fully onboarded, redirect to app
  const isFullyOnboarded = hasCompletedAccountSetup && hasCompletedProfileSetup;
  if (isFullyOnboarded) {
    window.location.href = loggedInPath;
    return null;
  }

  return (
    <HorizontalHeroLayout>
      {step === "account" ? <AccountSetupForm onComplete={() => setStep("profile")} /> : <ProfileSetupForm />}
    </HorizontalHeroLayout>
  );
}

interface AccountSetupFormProps {
  onComplete: () => void;
}

function AccountSetupForm({ onComplete }: AccountSetupFormProps) {
  const [selectedLogoFile, setSelectedLogoFile] = useState<File | null>(null);

  const { tenantId } = import.meta.user_info_env;
  const { data: tenant, isLoading } = useTenant(tenantId ?? "");

  const saveMutation = useElectricMutation({
    mutationFn: async (data: { body: { name: string } }) => {
      if (selectedLogoFile) {
        const logoFormData = new FormData();
        logoFormData.append("file", selectedLogoFile);
        const { error: logoError } = await apiClient.POST("/api/account/tenants/current/update-logo", {
          body: logoFormData as unknown as { file: string | null }
        });
        if (logoError) {
          throw logoError;
        }
      }

      const { error } = await apiClient.PUT("/api/account/tenants/current", data);
      if (error) {
        throw error;
      }
    },
    utils: tenantCollection.utils,
    onSuccess: () => {
      onComplete();
    }
  });

  const isPending = saveMutation.isPending;

  return (
    <Form
      onSubmit={mutationSubmitter(saveMutation)}
      validationErrors={saveMutation.validationErrors}
      validationBehavior="aria"
      className="flex w-full max-w-[25rem] flex-col items-center gap-4"
    >
      <Link href="/" className="cursor-pointer">
        <img src={logoMarkUrl} className="size-12" alt={t`Logo`} />
      </Link>
      <h2>
        <Trans>Let's set up your account</Trans>
      </h2>
      <div className="text-center text-muted-foreground text-sm">
        <Trans>Add your account name and logo.</Trans>
      </div>

      {isLoading ? (
        <div className="flex w-full flex-col gap-4">
          <div className="flex items-start gap-4">
            <Skeleton className="size-16 rounded-md" />
            <Skeleton className="h-16 flex-1" />
          </div>
          <Skeleton className="mt-4 h-11 w-full" />
        </div>
      ) : (
        <>
          <AccountFields
            autoFocus={true}
            tenant={tenant}
            isPending={isPending}
            onLogoFileSelect={setSelectedLogoFile}
          />

          <Button type="submit" disabled={isPending} className="mt-4 w-full">
            {isPending ? <Trans>Saving...</Trans> : <Trans>Continue</Trans>}
          </Button>
        </>
      )}
    </Form>
  );
}

function ProfileSetupForm() {
  const [selectedAvatarFile, setSelectedAvatarFile] = useState<File | null>(null);

  const { id: userId } = import.meta.user_info_env;
  const { data: user, isLoading } = useUser(userId ?? "");

  const saveMutation = useElectricMutation({
    mutationFn: async (data: { body: { firstName: string; lastName: string; title: string } }) => {
      if (selectedAvatarFile) {
        const avatarFormData = new FormData();
        avatarFormData.append("file", selectedAvatarFile);
        const { error: avatarError } = await apiClient.POST("/api/account/users/me/update-avatar", {
          body: avatarFormData as unknown as { file: string | null }
        });
        if (avatarError) {
          throw avatarError;
        }
      }

      const { error } = await apiClient.PUT("/api/account/users/me", data);
      if (error) {
        throw error;
      }
    },
    utils: userCollection.utils,
    onSuccess: () => {
      const returnPath = new URLSearchParams(window.location.search).get("returnPath");
      window.location.href = returnPath || loggedInPath;
    }
  });

  const isPending = saveMutation.isPending;

  return (
    <Form
      onSubmit={mutationSubmitter(saveMutation)}
      validationErrors={saveMutation.validationErrors}
      validationBehavior="aria"
      className="flex w-full max-w-[25rem] flex-col items-center gap-4"
    >
      <Link href="/" className="cursor-pointer">
        <img src={logoMarkUrl} className="size-12" alt={t`Logo`} />
      </Link>
      <h2>
        <Trans>Let's set up your profile</Trans>
      </h2>
      <div className="text-center text-muted-foreground text-sm">
        <Trans>Tell us a bit about yourself to get started.</Trans>
      </div>

      {isLoading ? (
        <div className="flex w-full flex-col gap-4">
          <div className="flex justify-center pb-4">
            <Skeleton className="size-20 rounded-full" />
          </div>
          <div className="flex gap-4">
            <Skeleton className="h-16 flex-1" />
            <Skeleton className="h-16 flex-1" />
          </div>
          <Skeleton className="h-16 w-full" />
          <Skeleton className="h-16 w-full" />
          <Skeleton className="mt-4 h-11 w-full" />
        </div>
      ) : (
        <>
          <div className="flex w-full flex-col gap-4">
            <UserProfileFields
              autoFocus={true}
              user={user}
              isPending={isPending}
              onAvatarFileSelect={setSelectedAvatarFile}
            />
          </div>

          <Button type="submit" disabled={isPending} className="mt-4 w-full">
            {isPending ? <Trans>Saving...</Trans> : <Trans>Continue</Trans>}
          </Button>
        </>
      )}
    </Form>
  );
}
