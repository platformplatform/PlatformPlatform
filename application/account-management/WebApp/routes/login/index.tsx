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
import { useFormState } from "react-dom";
import { useLingui } from "@lingui/react";
import { api } from "@/shared/lib/api/client";
import { FormErrorMessage } from "@repo/ui/components/FormErrorMessage";
import { setVerificationInfo } from "./-shared/verificationState";
import { useState } from "react";

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
  const { i18n } = useLingui();
  const [email, setEmail] = useState("");

  const [{ data, errors, success, title, message }, action, isPending] = useFormState(
    api.action("/api/account-management/authentication/start"),
    { success: null }
  );

  if (success) {
    const { loginId: id, validForSeconds } = data;

    setVerificationInfo({
      id,
      email,
      expireAt: new Date(Date.now() + validForSeconds * 1000)
    });
    return <Navigate to="/login/verify" />;
  }

  return (
    <Form
      action={action}
      validationErrors={errors}
      validationBehavior="aria"
      className="flex w-full max-w-sm flex-col items-center gap-4 space-y-3 px-6 pt-8 pb-4"
    >
      <Link href="/">
        <img src={logoMarkUrl} className="h-12 w-12" alt="logo mark" />
      </Link>
      <Heading className="text-2xl">Hi! Welcome back</Heading>
      <div className="text-center text-muted-foreground text-sm">Enter your email below to sign in</div>
      <TextField
        name="email"
        type="email"
        label={i18n.t("Email")}
        autoFocus
        isRequired
        value={email}
        onChange={setEmail}
        autoComplete="email webauthn"
        placeholder={i18n.t("yourname@example.com")}
        className="flex w-full flex-col"
      />
      <FormErrorMessage title={title} message={message} />
      <Button type="submit" isDisabled={isPending} className="mt-4 w-full text-center">
        Continue
      </Button>
      <p className="text-muted-foreground text-xs">
        Don't have an account? <Link href="/register">Create one</Link>
      </p>
      <img src={poweredByUrl} alt="powered by" />
    </Form>
  );
}
