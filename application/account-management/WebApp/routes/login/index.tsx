import { ErrorMessage } from "@/shared/components/ErrorMessage";
import logoMarkUrl from "@/shared/images/logo-mark.svg";
import poweredByUrl from "@/shared/images/powered-by.svg";
import { HorizontalHeroLayout } from "@/shared/layouts/HorizontalHeroLayout";
import { api } from "@/shared/lib/api/client";
import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { loggedInPath, signUpPath } from "@repo/infrastructure/auth/constants";
import { useIsAuthenticated } from "@repo/infrastructure/auth/hooks";
import { Button } from "@repo/ui/components/Button";
import { Form } from "@repo/ui/components/Form";
import { FormErrorMessage } from "@repo/ui/components/FormErrorMessage";
import { Heading } from "@repo/ui/components/Heading";
import { Link } from "@repo/ui/components/Link";
import { TextField } from "@repo/ui/components/TextField";
import { mutationSubmitter } from "@repo/ui/forms/mutationSubmitter";
import { Navigate, createFileRoute } from "@tanstack/react-router";
import { useState } from "react";
import { setLoginState } from "./-shared/loginState";

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
      onSubmit={mutationSubmitter(startLoginMutation)}
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
        autoFocus={true}
        isRequired={true}
        value={email}
        onChange={setEmail}
        autoComplete="email webauthn"
        placeholder={t`yourname@example.com`}
        className="flex w-full flex-col"
      />
      <FormErrorMessage error={startLoginMutation.error} />
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
