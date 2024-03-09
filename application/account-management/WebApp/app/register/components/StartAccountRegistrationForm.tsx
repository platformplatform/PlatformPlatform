import { useFormState, useFormStatus } from "react-dom";
import { DotIcon } from "lucide-react";
import { Trans } from "@lingui/macro";
import { useLingui } from "@lingui/react";
import { TextField } from "react-aria-components";
import type { State } from "./actions.ts";
import { startAccountRegistration } from "./actions.ts";
import { Button } from "@/ui/components/Button";
import { Form } from "@/ui//components/Form";
import { Link } from "@/ui/components/Link";
import { FieldError, Input, Label } from "@/ui/components/Field";
import poweredByUrl from "@/ui/images/powered-by.png";
import logoMarkUrl from "@/ui/images/logo-mark.png";

export function StartAccountRegistrationForm() {
  const { i18n } = useLingui();
  const initialState: State = { message: null, errors: {} };

  const [state, action] = useFormState(startAccountRegistration, initialState);

  return (
    <Form action={action} validationErrors={state.errors} className="space-y-3 w-full max-w-sm">
      <div className="flex flex-col gap-4 rounded-lg px-6 pb-4 pt-8 w-full">
        <div className="flex justify-center">
          <img src={logoMarkUrl} className="h-12 w-12" alt="logo mark" />
        </div>
        <h1 className="mb-3 text-2xl w-full text-center">
          Create your account
        </h1>
        <div className="text-gray-500 text-sm text-center">
          Sign up in seconds to get started building on PlatformPlatform - just like thousands of other developers.
        </div>
        <div className="w-full flex flex-col gap-4">
          <TextField className="flex flex-col">
            <Label>
              <Trans>Email</Trans>
            </Label>
            <Input
              type="email"
              name="email"
              autoFocus
              autoComplete="email webauthn"
              required
              placeholder={i18n.t("yourname@example.com")}
            />
            <FieldError />
          </TextField>
          <TextField className="flex flex-col">
            <Label>
              <Trans>Subdomain</Trans>
            </Label>
            <Input
              type="text"
              name="subdomain"
              required
              placeholder={i18n.t("subdomain")}
            />
            <FieldError />
          </TextField>
        </div>
        <StartAccountRegistrationButton />
        <div className="flex flex-col text-neutral-500 items-center gap-6">
          <p className="text-xs ">
            <Trans>Already have an account?</Trans>
            {" "}
            <Link href="/login" bold><Trans>Sign in</Trans></Link>
          </p>
          <div className="text-sm text-neutral-500">
            By continuing, you agree to our policies
            <div className="flex items-center justify-center">
              <Link href="/terms" bold>Terms of use</Link>
              <DotIcon className="w-4 h-4" />
              <Link href="/privacy" bold>Privacy Policies</Link>
            </div>
          </div>
          <img src={poweredByUrl} alt="powered by" />
        </div>
      </div>
    </Form>
  );
}

function StartAccountRegistrationButton() {
  const { pending } = useFormStatus();

  return (
    <Button type="submit" className="mt-4 w-full text-center" aria-disabled={pending}>
      <Trans>Create your account</Trans>
    </Button>
  );
}
