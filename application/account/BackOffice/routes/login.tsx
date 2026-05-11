import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { NotFoundError } from "@repo/infrastructure/auth/routeGuards";
import { Button } from "@repo/ui/components/Button";
import { Field, FieldContent, FieldDescription, FieldLabel, FieldTitle } from "@repo/ui/components/Field";
import { Link } from "@repo/ui/components/Link";
import { RadioGroup, RadioGroupItem } from "@repo/ui/components/RadioGroup";
import { createFileRoute } from "@tanstack/react-router";
import { useState } from "react";

import logoMarkUrl from "@/shared/images/logo-mark.svg";
import { HorizontalHeroLayout } from "@/shared/layouts/HorizontalHeroLayout";

const MOCK_IDENTITY_IDS = ["admin", "user"] as const;

interface MockLoginSearch {
  returnPath: string;
}

export const Route = createFileRoute("/login")({
  staticData: { trackingTitle: "Mock login" },
  beforeLoad: () => {
    if (process.env.NODE_ENV === "production") {
      throw new NotFoundError();
    }
  },
  validateSearch: (search: Record<string, unknown>): MockLoginSearch => {
    const raw = search.returnPath;
    const returnPath = typeof raw === "string" && raw.startsWith("/") ? raw : "/";
    return { returnPath };
  },
  component: MockLoginPage
});

function MockLoginPage() {
  const { returnPath } = Route.useSearch();
  const [selectedId, setSelectedId] = useState<string>("admin");
  const [isPending, setIsPending] = useState(false);

  return (
    <HorizontalHeroLayout>
      <form
        onSubmit={(event) => {
          event.preventDefault();
          setIsPending(true);
          const encodedReturnPath = encodeURIComponent(returnPath);
          globalThis.location.href = `/.auth/login/aad/callback?identity=${selectedId}&post_login_redirect_uri=${encodedReturnPath}`;
        }}
        className="flex w-full max-w-[22rem] flex-col items-center gap-4 pt-8 pb-4"
      >
        <Link href="/" className="cursor-pointer">
          <img src={logoMarkUrl} className="size-12" alt={t`Logo`} />
        </Link>
        <h2>
          <Trans>Back Office - Localhost</Trans>
        </h2>
        <div className="text-center text-sm text-muted-foreground">
          <Trans>
            Local development sign-in. Production uses Azure Container Apps built-in Entra ID authentication.
          </Trans>
        </div>

        <RadioGroup className="w-full pt-2" value={selectedId} onValueChange={setSelectedId}>
          {MOCK_IDENTITY_IDS.map((id) => (
            <FieldLabel key={id}>
              <Field orientation="horizontal">
                <RadioGroupItem value={id} />
                <FieldContent>
                  <FieldTitle>{getIdentityName(id)}</FieldTitle>
                  <FieldDescription>{getIdentityDescription(id)}</FieldDescription>
                </FieldContent>
              </Field>
            </FieldLabel>
          ))}
        </RadioGroup>

        <Button type="submit" isPending={isPending} className="mt-4 w-full text-center">
          {isPending ? <Trans>Logging in...</Trans> : <Trans>Log in</Trans>}
        </Button>
      </form>
    </HorizontalHeroLayout>
  );
}

function getIdentityName(id: string) {
  switch (id) {
    case "admin":
      return <Trans>Admin</Trans>;
    case "user":
      return <Trans>User</Trans>;
    default:
      return id;
  }
}

function getIdentityDescription(id: string) {
  switch (id) {
    case "admin":
      return <Trans>Log in with admin rights</Trans>;
    case "user":
      return <Trans>Log in without group claims</Trans>;
    default:
      return null;
  }
}
