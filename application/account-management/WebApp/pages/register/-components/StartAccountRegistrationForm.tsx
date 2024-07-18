import { useFormStatus } from "react-dom";
import { DotIcon } from "lucide-react";
import { Trans } from "@lingui/macro";
import { useLingui } from "@lingui/react";
import { TextField } from "react-aria-components";
import { Navigate } from "@tanstack/react-router";
import { useActionState } from "react";
import type { State } from "./actions";
import { startAccountRegistration } from "./actions";
import { Button } from "@repo/ui/components/Button";
import { Form } from "@repo/ui/components/Form";
import { Link } from "@repo/ui/components/Link";
import { FieldError } from "@repo/ui/components/FieldError";
import { Input } from "@repo/ui/components/Input";
import { Label } from "@repo/ui/components/Label";
import poweredByUrl from "../../../../../shared-webapp/ui/images/powered-by.svg";
import logoMarkUrl from "../../../../../shared-webapp/ui/images/logo-mark.svg";
import { DomainInput } from "@/shared/ui/DomainInput";
import { Select, SelectItem } from "@repo/ui/components/Select";

export function StartAccountRegistrationForm() {
  const { i18n } = useLingui();
  const initialState: State = { message: null, errors: {} };

  const [state, action] = useActionState(startAccountRegistration, initialState);

  if (state.success) {
    return <Navigate to="/register/verify" />;
  }

  return (
    <Form action={action} validationErrors={state.errors} className="space-y-3 w-full max-w-sm">
      <div className="flex flex-col gap-4 rounded-lg px-6 pb-4 pt-8 w-full">
        <div className="flex justify-center">
          <img src={logoMarkUrl} className="h-12 w-12" alt="logo mark" />
        </div>
        <h1 className="mb-3 text-2xl w-full text-center">Create your account</h1>
        <div className="text-gray-500 text-xs text-center">
          Sign up in seconds to get started building on PlatformPlatform - just like thousands of others.
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
            <DomainInput
              type="text"
              name="subdomain"
              domain=".platformplatform.net"
              required
              placeholder={i18n.t("subdomain")}
            />
            <FieldError />
          </TextField>
          <TextField className="flex flex-col">
            <Label>
              <Trans>Region</Trans>
            </Label>
            <Select name="region" selectedKey="europe" key="europe">
              <SelectItem id="europe">Europe</SelectItem>
            </Select>
            <FieldError />
          </TextField>
          <p className="text-gray-500 text-xs">
            <Trans>This is the region where your data is stored</Trans>{" "}
          </p>
        </div>
        <StartAccountRegistrationButton />
        <div className="flex flex-col text-neutral-500 items-center gap-6">
          <p className="text-xs ">
            <Trans>Already have an account?</Trans>{" "}
            <Link href="/login">
              <Trans>Log in</Trans>
            </Link>
          </p>
          <div className="text-sm text-neutral-500">
            By continuing, you agree to our policies
            <div className="flex items-center justify-center">
              <Link href="/">Terms of use</Link>
              <DotIcon className="w-4 h-4" />
              <Link href="/">Privacy Policies</Link>
            </div>
          </div>
          <img src={poweredByUrl} alt="powered by" className="w-28" />
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
