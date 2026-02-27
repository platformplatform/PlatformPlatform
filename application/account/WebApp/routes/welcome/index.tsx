import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { AuthenticationContext } from "@repo/infrastructure/auth/AuthenticationProvider";
import { loggedInPath } from "@repo/infrastructure/auth/constants";
import { Button } from "@repo/ui/components/Button";
import { Form } from "@repo/ui/components/Form";
import { Link } from "@repo/ui/components/Link";
import { Skeleton } from "@repo/ui/components/Skeleton";
import { mutationSubmitter } from "@repo/ui/forms/mutationSubmitter";
import type { FileUploadMutation } from "@repo/ui/types/FileUpload";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { createFileRoute } from "@tanstack/react-router";
import { useContext, useState } from "react";
import ErrorPage from "@/federated-modules/errorPages/ErrorPage";
import { AccountFields } from "@/shared/components/AccountFields";
import { UserProfileFields } from "@/shared/components/UserProfileFields";
import logoMarkUrl from "@/shared/images/logo-mark.svg";
import { HorizontalHeroLayout } from "@/shared/layouts/HorizontalHeroLayout";
import { api, type Schemas } from "@/shared/lib/api/client";

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

  const { data: tenant, isLoading } = api.useQuery("get", "/api/account/tenants/current");

  const updateTenantMutation = api.useMutation("put", "/api/account/tenants/current");
  const updateTenantLogoMutation = api.useMutation("post", "/api/account/tenants/current/update-logo");

  const saveMutation = useMutation<void, Schemas["HttpValidationProblemDetails"], { body: { name: string } }>({
    mutationFn: async (data) => {
      // Upload logo if selected
      if (selectedLogoFile) {
        const logoFormData = new FormData();
        logoFormData.append("file", selectedLogoFile);
        await (updateTenantLogoMutation as unknown as FileUploadMutation).mutateAsync({ body: logoFormData });
      }

      // Update tenant name
      await updateTenantMutation.mutateAsync({ body: data.body });
    },
    onSuccess: () => {
      onComplete();
    }
  });

  const isPending = saveMutation.isPending;

  return (
    <Form
      onSubmit={mutationSubmitter(saveMutation)}
      validationErrors={saveMutation.error?.errors}
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
  const queryClient = useQueryClient();
  const { updateUserInfo } = useContext(AuthenticationContext);

  const [selectedAvatarFile, setSelectedAvatarFile] = useState<File | null>(null);

  const { data: user, isLoading, refetch: refetchUser } = api.useQuery("get", "/api/account/users/me");

  const updateAvatarMutation = api.useMutation("post", "/api/account/users/me/update-avatar");
  const updateCurrentUserMutation = api.useMutation("put", "/api/account/users/me");

  const saveMutation = useMutation<
    void,
    Schemas["HttpValidationProblemDetails"],
    { body: { firstName: string; lastName: string; title: string } }
  >({
    mutationFn: async (data) => {
      const { firstName, lastName, title } = data.body;

      // Upload avatar if selected
      if (selectedAvatarFile) {
        const avatarFormData = new FormData();
        avatarFormData.append("file", selectedAvatarFile);
        await (updateAvatarMutation as unknown as FileUploadMutation).mutateAsync({ body: avatarFormData });
      }

      // Update user profile
      await updateCurrentUserMutation.mutateAsync({
        body: { firstName, lastName, title }
      });

      // Refresh user info
      const { data: updatedUser } = await refetchUser();
      if (updatedUser) {
        updateUserInfo(updatedUser);
      }

      await queryClient.invalidateQueries();
    },
    onSuccess: () => {
      const returnPath = new URLSearchParams(window.location.search).get("returnPath");
      window.location.href = returnPath || loggedInPath;
    }
  });

  const isPending = saveMutation.isPending;

  return (
    <Form
      onSubmit={mutationSubmitter(saveMutation)}
      validationErrors={saveMutation.error?.errors}
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
