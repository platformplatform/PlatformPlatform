import { useFormStatus } from "react-dom";
import { Trans } from "@lingui/macro";
import { useLingui } from "@lingui/react";
import { TextField } from "react-aria-components";
import { useActionState } from "react";
import { Button } from "@repo/ui/components/Button";
import { Form } from "@repo/ui/components/Form";
import { Link } from "@repo/ui/components/Link";
import { FieldError } from "@repo/ui/components/FieldError";
import { Input } from "@repo/ui/components/Input";
import { Label } from "@repo/ui/components/Label";
import poweredByUrl from "../../../../../shared-webapp/ui/images/powered-by.svg";
import logoMarkUrl from "../../../../../shared-webapp/ui/images/logo-mark.svg";
import { type AuthenticationState, useLogInAction } from "@repo/infrastructure/auth/hooks";

export default function LoginForm() {
  const logInAction = useLogInAction();
  const { i18n } = useLingui();
  const initialState: AuthenticationState = { message: null, errors: {} };

  const [state, action] = useActionState(logInAction, initialState);

  return (
    <Form action={action} validationErrors={state.errors} className="space-y-3 w-full max-w-sm">
      <div className="flex flex-col gap-4 rounded-lg px-6 pb-4 pt-8 w-full">
        <div className="flex justify-center">
          <Link href="/">
            <img src={logoMarkUrl} className="h-12 w-12" alt="logo mark" />
          </Link>
        </div>
        <h1 className="mb-3 text-2xl w-full text-center">
          <Trans>Hi! Welcome back</Trans>
        </h1>
        <div className="text-gray-500 text-xs text-center">
          <Trans>Enter your email below to log in.</Trans>
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
              placeholder={i18n.t("yourname@example.com")}
              className="border py-2 rounded-lg border-gray-300"
              aria-label={i18n.t("Email")}
            />
            <FieldError />
          </TextField>
        </div>
        <LoginButton />
        <div className="flex flex-col text-neutral-500 items-center gap-6">
          <p className="text-xs text-neutral-500">
            <Trans>Don't have an account?</Trans>{" "}
            <Link href="/register">
              <Trans>Sign up</Trans>
            </Link>
          </p>
          <img src={poweredByUrl} alt="powered by" className="w-28" />
        </div>
      </div>
    </Form>
  );
}

function LoginButton() {
  const { pending } = useFormStatus();

  return (
    <Button type="submit" className="mt-4 w-full text-center" aria-disabled={pending}>
      <Trans>Continue</Trans>
    </Button>
  );
}
