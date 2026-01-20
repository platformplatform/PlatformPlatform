import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { loggedInPath, loginPath } from "@repo/infrastructure/auth/constants";
import { useIsAuthenticated } from "@repo/infrastructure/auth/hooks";
import { Button } from "@repo/ui/components/Button";
import { Field, FieldDescription, FieldLabel } from "@repo/ui/components/Field";
import { Form } from "@repo/ui/components/Form";
import { LabelWithTooltip } from "@repo/ui/components/LabelWithTooltip";
import { Link } from "@repo/ui/components/Link";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@repo/ui/components/Select";
import { TextField } from "@repo/ui/components/TextField";
import { mutationSubmitter } from "@repo/ui/forms/mutationSubmitter";
import { createFileRoute, Navigate } from "@tanstack/react-router";
import { DotIcon } from "lucide-react";
import { useState } from "react";
import FederatedErrorPage from "@/federated-modules/errorPages/FederatedErrorPage";
import logoMarkUrl from "@/shared/images/logo-mark.svg";
import logoWrapUrl from "@/shared/images/logo-wrap.svg";
import { HorizontalHeroLayout } from "@/shared/layouts/HorizontalHeroLayout";
import { api } from "@/shared/lib/api/client";
import { getLoginState } from "../login/-shared/loginState";
import { clearSignupState, getSignupState, setSignupState } from "./-shared/signupState";

export const Route = createFileRoute("/signup/")({
  component: function SignupRoute() {
    const isAuthenticated = useIsAuthenticated();

    if (isAuthenticated) {
      return <Navigate to={loggedInPath} />;
    }

    return (
      <HorizontalHeroLayout>
        <StartSignupForm />
      </HorizontalHeroLayout>
    );
  },
  errorComponent: FederatedErrorPage
});

export function StartSignupForm() {
  const { email: savedEmail } = getSignupState();
  const { email: loginEmail } = getLoginState(); // Prefill from login page if user navigated here
  const [email, setEmail] = useState(savedEmail || loginEmail || "");

  const startSignupMutation = api.useMutation("post", "/api/account-management/signups/start");

  if (startSignupMutation.isSuccess) {
    const { emailConfirmationId, validForSeconds } = startSignupMutation.data;

    clearSignupState();
    setSignupState({
      emailConfirmationId,
      email,
      expireAt: new Date(Date.now() + validForSeconds * 1000)
    });

    return <Navigate to="/signup/verify" />;
  }

  return (
    <Form
      onSubmit={mutationSubmitter(startSignupMutation)}
      validationErrors={startSignupMutation.error?.errors}
      validationBehavior="aria"
      className="flex w-full max-w-sm flex-col items-center gap-4 space-y-3 rounded-lg px-6 pt-8 pb-4"
    >
      <Link href="/" className="cursor-pointer">
        <img src={logoMarkUrl} className="size-12" alt={t`Logo`} />
      </Link>
      <h2>
        <Trans>Create your account</Trans>
      </h2>
      <div className="text-center text-muted-foreground text-sm">
        <Trans>Sign up in seconds to start building on PlatformPlatform – just like thousands of others.</Trans>
      </div>
      <TextField
        name="email"
        type="email"
        label={t`Email`}
        autoFocus={true}
        isRequired={true}
        value={email}
        onChange={setEmail}
        autoComplete="email webauthn"
        placeholder={t`yourname@example.com`}
        className="flex w-full flex-col"
      />
      <Field className="flex w-full flex-col">
        <FieldLabel>
          <LabelWithTooltip
            tooltip={t`Data storage location for privacy and compliance. This cannot be changed later.`}
          >
            {t`Region`}
          </LabelWithTooltip>
        </FieldLabel>
        <Select name="region" defaultValue="europe" required={true}>
          <SelectTrigger className="w-full" aria-label={t`Region`}>
            <SelectValue>{() => <Trans>Europe</Trans>}</SelectValue>
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="europe">
              <Trans>Europe</Trans>
            </SelectItem>
          </SelectContent>
        </Select>
        <FieldDescription>{t`This is the region where your data is stored`}</FieldDescription>
      </Field>
      <Button type="submit" disabled={startSignupMutation.isPending} className="mt-4 w-full text-center">
        {startSignupMutation.isPending ? (
          <Trans>Sending verification code...</Trans>
        ) : (
          <Trans>Create your account</Trans>
        )}
      </Button>
      <p className="text-muted-foreground text-sm">
        <Trans>Do you already have an account?</Trans>{" "}
        <Link href={loginPath}>
          <Trans>Log in</Trans>
        </Link>
      </p>
      <div className="text-muted-foreground text-sm">
        <Trans>By continuing, you accept our policies</Trans>
        <div className="flex items-center justify-center">
          <Link href="/legal/terms">
            <Trans>Terms of use</Trans>
          </Link>
          <DotIcon className="size-4" />
          <Link href="/legal/privacy">
            <Trans>Privacy policies</Trans>
          </Link>
        </div>
      </div>
      {/*
        Built with PlatformPlatform - https://github.com/platformplatform/PlatformPlatform
        We'd appreciate it if you keep this attribution to help others discover this free, open-source platform. Thank you! 🙏
      */}
      <div className="flex flex-col items-center gap-1">
        <span className="text-muted-foreground text-xs">
          <Trans>Built with</Trans>
        </span>
        <Link href="https://github.com/platformplatform/PlatformPlatform" className="cursor-pointer">
          <img src={logoWrapUrl} alt={t`PlatformPlatform`} className="h-6 w-auto" />
        </Link>
      </div>
    </Form>
  );
}
