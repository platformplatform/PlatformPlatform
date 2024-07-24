import { createFileRoute } from "@tanstack/react-router";
import { HorizontalHeroLayout } from "@/shared/layouts/HorizontalHeroLayout";
import { ErrorMessage } from "@/shared/components/ErrorMessage";

import { Button } from "@repo/ui/components/Button";
import { Form } from "@repo/ui/components/Form";
import { Heading } from "@repo/ui/components/Heading";
import { Link } from "@repo/ui/components/Link";
import logoMarkUrl from "@/shared/images/logo-mark.svg";
import poweredByUrl from "@/shared/images/powered-by.svg";
import { type AuthenticationState, useLogInAction } from "@repo/infrastructure/auth/hooks";
import { TextField } from "@repo/ui/components/TextField";
import { useFormState } from "react-dom";
import { useLingui } from "@lingui/react";

export const Route = createFileRoute("/login/")({
  component: () => (
    <HorizontalHeroLayout>
      <LoginForm />
    </HorizontalHeroLayout>
  ),
  errorComponent: (props) => (
    <HorizontalHeroLayout>
      <ErrorMessage {...props} />
    </HorizontalHeroLayout>
  )
});

export function LoginForm() {
  const logInAction = useLogInAction();
  const { i18n } = useLingui();
  const initialState: AuthenticationState = { message: null, errors: {} };

  const [state, action] = useFormState(logInAction, initialState);

  return (
    <Form
      action={action}
      validationErrors={state.errors}
      validationBehavior="aria"
      className="flex w-full max-w-sm flex-col items-center gap-4 space-y-3 px-6 pt-8 pb-4"
    >
      <Link href="/">
        <img src={logoMarkUrl} className="h-12 w-12" alt="logo mark" />
      </Link>
      <Heading className="text-2xl">Hi! Welcome back</Heading>
      <div className="text-center text-muted-foreground text-sm">Enter your email below to sign in</div>
      <TextField
        type="email"
        name="email"
        label="Email"
        autoComplete="email webauthn"
        autoFocus
        isRequired
        placeholder={i18n.t("yourname@example.com")}
        className="flex w-full flex-col"
      />
      <Button type="submit" className="mt-4 w-full text-center">
        Continue
      </Button>
      <p className="text-muted-foreground text-xs">
        Don't have an account? <Link href="/register">Create one</Link>
      </p>
      <img src={poweredByUrl} alt="powered by" />
    </Form>
  );
}
