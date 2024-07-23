import { DotIcon } from "lucide-react";
import { Trans } from "@lingui/macro";
import { useLingui } from "@lingui/react";
import { Navigate } from "@tanstack/react-router";
import { Button } from "@repo/ui/components/Button";
import { Heading } from "@repo/ui/components/Heading";
import { Link } from "@repo/ui/components/Link";
import { Select, SelectItem } from "@repo/ui/components/Select";
import { DomainInputField } from "@repo/ui/components/DomainInputField";
import logoMarkUrl from "@/shared/images/logo-mark.svg";
import poweredByUrl from "@/shared/images/powered-by.svg";
import { startAccountRegistration, type State } from "./actions";
import { TextField } from "@repo/ui/components/TextField";
import { Form } from "@repo/ui/components/Form";
import { useActionState } from "react";

export function StartAccountRegistrationForm() {
  const { i18n } = useLingui();
  const initialState: State = { message: null, errors: {} };

  const [{ errors, success }, action, isPending] = useActionState(startAccountRegistration, initialState);

  if (success) {
    return <Navigate to="/register/verify" />;
  }

  return (
    <Form
      action={action}
      validationErrors={errors}
      validationBehavior="aria"
      className="flex w-full max-w-sm flex-col items-center gap-4 space-y-3 rounded-lg px-6 pt-8 pb-4"
    >
      <Link href="/">
        <img src={logoMarkUrl} className="h-12 w-12" alt="logo mark" />
      </Link>
      <Heading className="text-2xl">Create your account</Heading>
      <div className="text-center text-muted-foreground text-sm">
        Sign up in seconds to get started building on PlatformPlatform - just like thousands of others.
      </div>
      <TextField
        name="email"
        type="email"
        label={i18n.t("Email")}
        autoFocus
        isRequired
        autoComplete="email webauthn"
        placeholder={i18n.t("yourname@example.com")}
        className="flex w-full flex-col"
      />
      <DomainInputField
        name="subdomain"
        domain=".platformplatform.net"
        label={i18n.t("Subdomain")}
        placeholder={i18n.t("subdomain")}
        isRequired
        className="flex w-full flex-col"
      />
      <Select
        name="region"
        selectedKey="europe"
        label={i18n.t("Region")}
        description={i18n.t("This is the region where your data is stored")}
        isRequired
        className="flex w-full flex-col"
      >
        <SelectItem id="europe">Europe</SelectItem>
      </Select>
      <Button type="submit" isDisabled={isPending} className="mt-4 w-full text-center">
        Create your account
      </Button>
      <p className="text-muted-foreground text-xs">
        <Trans>Already have an account?</Trans>
        <Link href="/login">
          <Trans>Log in</Trans>
        </Link>
      </p>
      <div className="text-muted-foreground text-sm">
        By continuing, you agree to our policies
        <div className="flex items-center justify-center">
          <Link href="/">Terms of use</Link>
          <DotIcon className="h-4 w-4" />
          <Link href="/">Privacy Policies</Link>
        </div>
      </div>
      <img src={poweredByUrl} alt="powered by" />
    </Form>
  );
}
