import { createFileRoute, Navigate } from "@tanstack/react-router";
import { HorizontalHeroLayout } from "@/shared/layouts/HorizontalHeroLayout";
import { ErrorMessage } from "@/shared/components/ErrorMessage";
import { Button } from "@repo/ui/components/Button";
import { Form } from "@repo/ui/components/Form";
import { Heading } from "@repo/ui/components/Heading";
import { Link } from "@repo/ui/components/Link";
import logoMarkUrl from "@/shared/images/logo-mark.svg";
import poweredByUrl from "@/shared/images/powered-by.svg";
import { TextField } from "@repo/ui/components/TextField";
import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { useState } from "react";
import { api } from "@/shared/lib/api/client";
import { setLoginState } from "./-shared/loginState";
import { GeneralFormErrorMessage } from "@repo/ui/components/GeneralFormErrorMessage";
import { loggedInPath, signUpPath } from "@repo/infrastructure/auth/constants";
import { useIsAuthenticated } from "@repo/infrastructure/auth/hooks";

export const Route = createFileRoute("/login/")({
  validateSearch: (search) => {
    const returnPath = search.returnPath as string | undefined;
    // Only allow paths starting with / to prevent open redirect attacks to external domains
    return {
      returnPath: returnPath?.startsWith("/") ? returnPath : undefined
    };
  },
  component: function LoginRoute() {
    const isAuthenticated = useIsAuthenticated();

    if (isAuthenticated) {
      return <Navigate to={loggedInPath} />;
    }

    return (
      <HorizontalHeroLayout>
        <LoginForm />
      </HorizontalHeroLayout>
    );
  },
  errorComponent: (props) => (
    <HorizontalHeroLayout>
      <ErrorMessage {...props} />
    </HorizontalHeroLayout>
  )
});

export function LoginForm() {
  const [email, setEmail] = useState("");
  const { returnPath } = Route.useSearch();

  const startLoginMutation = api.useMutation("post", "/api/account-management/authentication/login/start");

  const handleSubmit = (formData: FormData) => {
    // biome-ignore lint/suspicious/noExplicitAny: Same as we do in PlatformServerAction.ts
    startLoginMutation.mutate({ body: Object.fromEntries(formData) as any });
  };

  if (startLoginMutation.isSuccess) {
    const { loginId, emailConfirmationId, validForSeconds } = startLoginMutation.data;

    setLoginState({
      loginId,
      emailConfirmationId,
      email,
      expireAt: new Date(Date.now() + validForSeconds * 1000)
    });

    return <Navigate to="/login/verify" search={{ returnPath }} />;
  }

  return (
    <Form
      action={handleSubmit}
      validationErrors={startLoginMutation.error?.errors}
      validationBehavior="aria"
      className="flex w-full max-w-sm flex-col items-center gap-4 space-y-3 px-6 pt-8 pb-4"
    >
      <Link href="/">
        <img src={logoMarkUrl} className="h-12 w-12" alt={t`Logo`} />
      </Link>
      <Heading className="text-2xl">
        <Trans>Hi! Welcome back</Trans>
      </Heading>
      <div className="text-center text-muted-foreground text-sm">
        <Trans>Enter your email below to log in</Trans>
      </div>
      <TextField
        name="email"
        type="email"
        label={t`Email`}
        autoFocus
        isRequired
        value={email}
        onChange={setEmail}
        autoComplete="email webauthn"
        placeholder={t`yourname@example.com`}
        className="flex w-full flex-col"
      />
      <GeneralFormErrorMessage error={startLoginMutation.error} />
      <Button type="submit" isDisabled={startLoginMutation.isPending} className="mt-4 w-full text-center">
        <Trans>Continue</Trans>
      </Button>
      <div className="text-muted-foreground text-sm">
        <Trans>
          Don't have an account? <Link href={signUpPath}>Create one</Link>
        </Trans>
      </div>
      <img src={poweredByUrl} alt={t`Powered by`} />
    </Form>
  );
}
