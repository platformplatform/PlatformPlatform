import { createFileRoute, Navigate } from "@tanstack/react-router";
import { HorizontalHeroLayout } from "@/shared/layouts/HorizontalHeroLayout";
import { ErrorMessage } from "@/shared/components/ErrorMessage";
import { DotIcon } from "lucide-react";
import { Trans } from "@lingui/macro";
import { useLingui } from "@lingui/react";
import { Button } from "@repo/ui/components/Button";
import { Heading } from "@repo/ui/components/Heading";
import { Link } from "@repo/ui/components/Link";
import { Select, SelectItem } from "@repo/ui/components/Select";
import { DomainInputField } from "@repo/ui/components/DomainInputField";
import logoMarkUrl from "@/shared/images/logo-mark.svg";
import poweredByUrl from "@/shared/images/powered-by.svg";
import { TextField } from "@repo/ui/components/TextField";
import { Form } from "@repo/ui/components/Form";
import { useFormState } from "react-dom";
import { useState } from "react";
import { api, useApi } from "@/shared/lib/api/client";
import { setRegistration } from "./-shared/registrationState";
import { FormErrorMessage } from "@repo/ui/components/FormErrorMessage";

export const Route = createFileRoute("/register/")({
  component: () => (
    <HorizontalHeroLayout>
      <StartAccountRegistrationForm />
    </HorizontalHeroLayout>
  ),
  errorComponent: (props) => (
    <HorizontalHeroLayout>
      <ErrorMessage {...props} />
    </HorizontalHeroLayout>
  )
});

export function StartAccountRegistrationForm() {
  const { i18n } = useLingui();
  const [email, setEmail] = useState("");

  const [{ success, errors, data, title, message }, action, isPending] = useFormState(
    api.action("/api/account-management/account-registrations/start"),
    { success: null }
  );

  const [subdomain, setSubdomain] = useState("");
  const { data: isSubdomainFree } = useApi(
    "/api/account-management/account-registrations/is-subdomain-free",
    {
      params: {
        query: { Subdomain: subdomain }
      }
    },
    {
      autoFetch: subdomain.length > 3,
      debounceMs: 500
    }
  );

  if (success === true) {
    const { accountRegistrationId, validForSeconds } = data;

    setRegistration({
      accountRegistrationId,
      email,
      expireAt: new Date(Date.now() + validForSeconds * 1000)
    });

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
        value={email}
        onChange={setEmail}
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
        value={subdomain}
        onChange={setSubdomain}
        isSubdomainFree={isSubdomainFree}
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
      <FormErrorMessage title={title} message={message} />
      <Button type="submit" isDisabled={isPending} className="mt-4 w-full text-center">
        Create your account
      </Button>
      <p className="text-muted-foreground text-xs">
        <Trans>Already have an account?</Trans>{" "}
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
