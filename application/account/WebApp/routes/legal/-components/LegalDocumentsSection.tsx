import { Trans } from "@lingui/react/macro";
import { Card, CardDescription, CardTitle } from "@repo/ui/components/Card";
import { Link } from "@repo/ui/components/Link";
import { FileTextIcon, ScrollTextIcon, ShieldCheckIcon } from "lucide-react";

export function LegalDocumentsSection() {
  return (
    <section className="bg-background px-6 pt-12 pb-16">
      <div className="mx-auto max-w-5xl">
        <div className="mb-12 text-center">
          <h2 className="marketing">
            <Trans>Legal and Compliance</Trans>
          </h2>
          <p className="text-muted-foreground">
            <Trans>
              Transparency, security, and privacy are at the core of how we operate. Review our policies and learn how
              we protect your data.
            </Trans>
          </p>
        </div>

        <div className="grid gap-6 md:grid-cols-3">
          <Link
            href="/legal/terms"
            underline={false}
            className="block min-w-0 rounded-xl whitespace-normal outline-ring focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2"
          >
            <Card className="h-full px-6 transition-colors hover:bg-hover-background">
              <ScrollTextIcon className="mb-4 size-10 text-primary" />
              <CardTitle>
                <Trans>Terms of Service</Trans>
              </CardTitle>
              <CardDescription>
                <Trans>The agreement governing your use of our Service, including acceptable use and liability.</Trans>
              </CardDescription>
            </Card>
          </Link>

          <Link
            href="/legal/privacy"
            underline={false}
            className="block min-w-0 rounded-xl whitespace-normal outline-ring focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2"
          >
            <Card className="h-full px-6 transition-colors hover:bg-hover-background">
              <ShieldCheckIcon className="mb-4 size-10 text-primary" />
              <CardTitle>
                <Trans>Privacy Policy</Trans>
              </CardTitle>
              <CardDescription>
                <Trans>How we collect, use, and protect your personal data in compliance with GDPR.</Trans>
              </CardDescription>
            </Card>
          </Link>

          <Link
            href="/legal/dpa"
            underline={false}
            className="block min-w-0 rounded-xl whitespace-normal outline-ring focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2"
          >
            <Card className="h-full px-6 transition-colors hover:bg-hover-background">
              <FileTextIcon className="mb-4 size-10 text-primary" />
              <CardTitle>
                <Trans>Data Processing Agreement</Trans>
              </CardTitle>
              <CardDescription>
                <Trans>GDPR Article 28 compliant agreement for processing data on your behalf.</Trans>
              </CardDescription>
            </Card>
          </Link>
        </div>
      </div>
    </section>
  );
}
