import { useLingui } from "@lingui/react";
import { useFormState } from "react-dom";
import { Button } from "@repo/ui/components/Button";
import { FieldError } from "@repo/ui/components/FieldError";
import { Form } from "@repo/ui/components/Form";
import { Heading } from "@repo/ui/components/Heading";
import { Input } from "@repo/ui/components/Input";
import { Label } from "@repo/ui/components/Label";
import { Link } from "@repo/ui/components/Link";
import logoMarkUrl from "@/shared/images/logo-mark.svg";
import poweredByUrl from "@/shared/images/powered-by.svg";
import { type AuthenticationState, useLogInAction } from "@repo/infrastructure/auth/hooks";
import { TextField } from "@repo/ui/components/TextField";
import { createFileRoute } from "@tanstack/react-router";
import { HeroImage } from "@/shared/ui/images/HeroImage";

export const Route = createFileRoute("/login/")({
  component: LoginPage
});

export function LoginPage() {
  const logInAction = useLogInAction();
  const { i18n } = useLingui();
  const initialState: AuthenticationState = { message: null, errors: {} };

  const [state, action] = useFormState(logInAction, initialState);

  return (
    <main className="flex min-h-screen flex-col">
      <div className="flex grow flex-col gap-4 md:flex-row">
        <div className="flex flex-col items-center justify-center gap-6 md:w-1/2 p-6">
          <Form
            action={action}
            validationErrors={state.errors}
            className="flex w-full max-w-sm flex-col items-center gap-4 space-y-3 px-6 pt-8 pb-4"
          >
            <Link href="/">
              <img src={logoMarkUrl} className="h-12 w-12" alt="logo mark" />
            </Link>
            <Heading className="text-2xl">Hi! Welcome back</Heading>
            <div className="text-center text-muted-foreground text-sm">Enter your email below to sign in</div>
            <TextField className="flex w-full flex-col">
              <Label>Email</Label>
              <Input
                type="email"
                name="email"
                autoComplete="email webauthn"
                autoFocus
                required
                placeholder={i18n.t("yourname@example.com")}
              />
              <FieldError />
            </TextField>
            <span className="text-destructive text-sm" slot="errorMessage">
              {state.errors?.email ?? ""}
            </span>
            <Button type="submit" className="mt-4 w-full text-center">
              Continue
            </Button>
            <p className="text-muted-foreground text-xs">
              Don't have an account? <Link href="/register">Create one</Link>
            </p>
            <img src={poweredByUrl} alt="powered by" />
          </Form>
        </div>
        <div className="flex items-center justify-center p-6 bg-gray-50 md:w-1/2 md:px-28 md:py-12">
          <HeroImage />
        </div>
      </div>
    </main>
  );
}
