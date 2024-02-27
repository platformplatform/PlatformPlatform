/* eslint-disable react/no-unescaped-entities */
import { useFormState, useFormStatus } from "react-dom";
import { Trans } from "@lingui/macro";
import { useLingui } from "@lingui/react";
import { TextField } from "react-aria-components";
import { Button } from "../components/Button";
import { Form } from "../components/Form";
import { Link } from "@/lib/router/router";
import { FieldError, Input, Label } from "@/ui/components/Field";
import poweredByUrl from "@/ui/Auth/powered-by.png";
import logoMarkUrl from "@/ui/Auth/logo-mark.png";
import { useSignInAction } from "@/lib/auth/hooks";
import type { State } from "@/lib/auth/actions";

export default function LoginForm() {
  const signInAction = useSignInAction();
  const { i18n } = useLingui();
  const initialState: State = { message: null, errors: {} };

  const [state, action] = useFormState(signInAction, initialState);

  return (
    <Form action={action} validationErrors={state.errors} className="space-y-3 w-full max-w-sm">
      <div className="flex flex-col gap-4 rounded-lg px-6 pb-4 pt-8 w-full">
        <div className="flex justify-center">
          <img src={logoMarkUrl} className="h-12 w-12" alt="logo mark" />
        </div>
        <h1 className="mb-3 text-2xl w-full text-center">
          <Trans>
            Please sign in to continue
          </Trans>
        </h1>
        <div className="text-gray-500 text-sm text-center">
          <Trans>
            Sign in with your company email address to get started building on PlatformPlatform -
            just like thousands of other developers.
          </Trans>
        </div>
        <div className="w-full flex flex-col gap-4">
          <TextField className="flex flex-col">
            <Label>
              <Trans>Email</Trans>
            </Label>
            <Input
              type="email"
              name="email"
              autoComplete="email webauthn"
              autoFocus
              required
              placeholder={i18n.t("name@work.email.com")}
            />
            <FieldError />
          </TextField>

          <TextField type="password" name="password" className="flex flex-col">
            <Label>
              <Trans>Password</Trans>
            </Label>
            <Input
              type="password"
              name="password"
              autoComplete="current-password"
              placeholder={i18n.t("Enter password")}
              required
            />
            <FieldError />
          </TextField>
        </div>
        <LoginButton />
        <div className="flex flex-col items-center">
          <p className="text-xs text-neutral-500">
            <Trans>Don't have an account?</Trans>
            {" "}
            <Link to="/register" className="font-semibold"><Trans>Sign up</Trans></Link>
          </p>
          <img src={poweredByUrl} alt="powered by" />
        </div>
      </div>
    </Form>
  );
}

function LoginButton() {
  const { pending } = useFormStatus();

  return (
    <Button variant="neutral" type="submit" className="mt-4 w-full text-center" aria-disabled={pending}>
      <Trans>Sign in</Trans>
    </Button>
  );
}
