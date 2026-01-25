import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Card, CardDescription, CardTitle } from "@repo/ui/components/Card";
import { Link } from "@repo/ui/components/Link";
import { createFileRoute } from "@tanstack/react-router";
import {
  DatabaseIcon,
  ExternalLinkIcon,
  FileTextIcon,
  FingerprintIcon,
  KeyIcon,
  LayersIcon,
  ScrollTextIcon,
  ServerIcon,
  ShieldCheckIcon
} from "lucide-react";
import { PublicFooter } from "@/shared/components/PublicFooter";
import { PublicNavigation } from "@/shared/components/PublicNavigation";
import gdprBadge from "@/shared/images/compliance/gdpr.png";
import azureActivityLog from "@/shared/images/icons/azure-activity-log.svg";
import azureCompliance from "@/shared/images/icons/azure-compliance.svg";
import azurePolicy from "@/shared/images/icons/azure-policy.svg";
import azureSecurity from "@/shared/images/icons/azure-security.svg";
import platformLogo from "@/shared/images/logo-mark.svg";

export const Route = createFileRoute("/legal/")({
  component: LegalIndex
});

function LegalIndex() {
  return (
    <main className="flex min-h-screen w-full flex-col">
      <div className="flex flex-1 flex-col">
        <PublicNavigation />

        {/* Section 1: Legal and Compliance */}
        <section className="bg-background px-6 pt-12 pb-16">
          <div className="mx-auto max-w-5xl">
            {/* Hero */}
            <div className="mb-12 text-center">
              <h2 className="marketing">
                <Trans>Legal and Compliance</Trans>
              </h2>
              <p className="text-muted-foreground">
                <Trans>
                  Transparency, security, and privacy are at the core of how we operate. Review our policies and learn
                  how we protect your data.
                </Trans>
              </p>
            </div>

            {/* Legal Documents Cards */}
            <div className="grid gap-6 md:grid-cols-3">
              <Link
                href="/legal/terms"
                underline={false}
                className="block min-w-0 whitespace-normal rounded-xl outline-ring focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2"
              >
                <Card className="h-full px-6 transition-colors hover:bg-hover-background">
                  <ScrollTextIcon className="mb-4 size-10 text-primary" />
                  <CardTitle>
                    <Trans>Terms of Service</Trans>
                  </CardTitle>
                  <CardDescription>
                    <Trans>
                      The agreement governing your use of our Service, including acceptable use and liability.
                    </Trans>
                  </CardDescription>
                </Card>
              </Link>

              <Link
                href="/legal/privacy"
                underline={false}
                className="block min-w-0 whitespace-normal rounded-xl outline-ring focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2"
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
                className="block min-w-0 whitespace-normal rounded-xl outline-ring focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2"
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

        {/* Section 2: Enterprise-grade Azure Infrastructure */}
        <section className="bg-input-background px-6 py-16">
          <div className="mx-auto max-w-5xl">
            {/* Header */}
            <div className="mb-12 text-center">
              <h2 className="marketing">
                <Trans>Enterprise-grade Azure infrastructure</Trans>
              </h2>
              <p className="text-muted-foreground">
                <Trans>Microsoft Azure Platform-as-a-Service (PaaS) with enterprise-grade reliability.</Trans>
              </p>
            </div>

            {/* Compliance Badges */}
            <div className="mb-12 flex flex-wrap items-center justify-center gap-6 sm:gap-8 md:gap-12">
              <div className="flex flex-col items-center gap-3">
                <div className="flex size-24 items-center justify-center">
                  <img src={gdprBadge} alt={t`GDPR compliant`} className="h-20 w-auto object-contain" />
                </div>
                <span className="font-semibold text-foreground">
                  <Trans>GDPR Compliant</Trans>
                </span>
              </div>
              <div className="flex flex-col items-center gap-3">
                <div className="flex size-24 items-center justify-center rounded-full bg-[#0078d4]/10">
                  <svg viewBox="0 0 96 96" className="size-14" fill="none" xmlns="http://www.w3.org/2000/svg">
                    <title>Microsoft Azure</title>
                    <path d="M48 12L12 30v36l36 18 36-18V30L48 12z" fill="#0078d4" />
                    <path d="M48 12v36L12 30l36-18z" fill="#50e6ff" />
                    <path d="M48 48v36l36-18V30L48 48z" fill="#0078d4" />
                    <path d="M48 48L12 30v36l36 18V48z" fill="#1490df" />
                  </svg>
                </div>
                <span className="font-semibold text-foreground">
                  <Trans>Microsoft Azure</Trans>
                </span>
              </div>
            </div>

            {/* Security Features - 6 items in 3x2 grid */}
            <div className="mb-12 grid gap-6 md:grid-cols-2 lg:grid-cols-3">
              <Card className="px-6 transition-colors hover:bg-hover-background">
                <div className="mb-3 flex size-10 items-center justify-center rounded-lg bg-primary/10">
                  <ServerIcon className="size-6 text-primary" />
                </div>
                <CardTitle>
                  <Trans>Fully managed infrastructure</Trans>
                </CardTitle>
                <CardDescription>
                  <Trans>Microsoft patches and secures all PaaS infrastructure automatically</Trans>
                </CardDescription>
              </Card>
              <Card className="px-6 transition-colors hover:bg-hover-background">
                <div className="mb-3 flex size-10 items-center justify-center rounded-lg bg-primary/10">
                  <FingerprintIcon className="size-6 text-primary" />
                </div>
                <CardTitle>
                  <Trans>Managed identities</Trans>
                </CardTitle>
                <CardDescription>
                  <Trans>Only trusted personnel access data through Azure AD authentication</Trans>
                </CardDescription>
              </Card>
              <Card className="px-6 transition-colors hover:bg-hover-background">
                <div className="mb-3 flex size-10 items-center justify-center rounded-lg bg-primary/10">
                  <LayersIcon className="size-6 text-primary" />
                </div>
                <CardTitle>
                  <Trans>Environment isolation</Trans>
                </CardTitle>
                <CardDescription>
                  <Trans>Strict separation prevents production data from leaking to other environments</Trans>
                </CardDescription>
              </Card>
              <Card className="px-6 transition-colors hover:bg-hover-background">
                <div className="mb-3 flex size-10 items-center justify-center rounded-lg bg-primary/10">
                  <KeyIcon className="size-6 text-primary" />
                </div>
                <CardTitle>
                  <Trans>Passwordless deployments</Trans>
                </CardTitle>
                <CardDescription>
                  <Trans>GitHub deploys directly to Azure without passwords or API keys</Trans>
                </CardDescription>
              </Card>
              <Card className="px-6 transition-colors hover:bg-hover-background">
                <div className="mb-3 flex size-10 items-center justify-center rounded-lg bg-primary/10">
                  <DatabaseIcon className="size-6 text-primary" />
                </div>
                <CardTitle>
                  <Trans>Data residency</Trans>
                </CardTitle>
                <CardDescription>
                  <Trans>Your data stays in the Azure region you select at signup</Trans>
                </CardDescription>
              </Card>
              <Card className="px-6 transition-colors hover:bg-hover-background">
                <div className="mb-3 flex size-10 items-center justify-center rounded-lg bg-primary/10">
                  <ShieldCheckIcon className="size-6 text-primary" />
                </div>
                <CardTitle>
                  <Trans>100% Security Score</Trans>
                </CardTitle>
                <CardDescription>
                  <Trans>Achieved in Microsoft Defender for Cloud following Azure best practices</Trans>
                </CardDescription>
              </Card>
            </div>

            {/* PlatformPlatform Credit */}
            <div className="flex items-center justify-center gap-3">
              <img src={platformLogo} alt={t`PlatformPlatform logo`} className="size-10" loading="lazy" />
              <p className="text-muted-foreground text-sm">
                <Trans>
                  Built on{" "}
                  <Link
                    href="https://github.com/platformplatform/PlatformPlatform"
                    target="_blank"
                    rel="noopener noreferrer"
                    aria-label={t`PlatformPlatform on GitHub (opens in new window)`}
                    className="inline-flex items-center gap-1 font-medium text-primary"
                  >
                    PlatformPlatform
                    <ExternalLinkIcon className="size-3" />
                  </Link>{" "}
                  - an open-source platform by industry experts showcasing how to build enterprise-grade B2B SaaS
                  products
                </Trans>
              </p>
            </div>
          </div>
        </section>

        {/* Section 3: Microsoft Compliance Resources */}
        <section className="bg-background px-6 py-16">
          <div className="mx-auto max-w-5xl">
            <div className="mb-12 text-center">
              <h2 className="marketing">
                <Trans>Microsoft compliance resources</Trans>
              </h2>
              <p className="text-muted-foreground">
                <Trans>
                  For detailed compliance certifications and audit reports, visit Microsoft's official resources.
                </Trans>
              </p>
            </div>
            <div className="grid gap-4 md:grid-cols-2">
              <Link
                href="https://www.microsoft.com/en-us/trust-center"
                target="_blank"
                rel="noopener noreferrer"
                aria-label={t`Trust Center (opens in new window)`}
                underline={false}
                className="block min-w-0 whitespace-normal rounded-xl outline-ring focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2"
              >
                <Card className="h-full gap-3 px-6 py-5 transition-colors hover:bg-hover-background">
                  <img src={azureSecurity} alt="" className="size-10" />
                  <CardTitle className="flex items-center gap-2">
                    <Trans>Trust Center</Trans>
                    <ExternalLinkIcon className="size-4 text-muted-foreground" />
                  </CardTitle>
                  <CardDescription>
                    <Trans>Security, privacy, and compliance information</Trans>
                  </CardDescription>
                </Card>
              </Link>
              <Link
                href="https://learn.microsoft.com/en-us/azure/compliance/"
                target="_blank"
                rel="noopener noreferrer"
                aria-label={t`Azure Compliance (opens in new window)`}
                underline={false}
                className="block min-w-0 whitespace-normal rounded-xl outline-ring focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2"
              >
                <Card className="h-full gap-3 px-6 py-5 transition-colors hover:bg-hover-background">
                  <img src={azureCompliance} alt="" className="size-10" />
                  <CardTitle className="flex items-center gap-2">
                    <Trans>Azure Compliance</Trans>
                    <ExternalLinkIcon className="size-4 text-muted-foreground" />
                  </CardTitle>
                  <CardDescription>
                    <Trans>Regulatory standards and certifications</Trans>
                  </CardDescription>
                </Card>
              </Link>
              <Link
                href="https://servicetrust.microsoft.com/"
                target="_blank"
                rel="noopener noreferrer"
                aria-label={t`SOC and ISO Reports (opens in new window)`}
                underline={false}
                className="block min-w-0 whitespace-normal rounded-xl outline-ring focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2"
              >
                <Card className="h-full gap-3 px-6 py-5 transition-colors hover:bg-hover-background">
                  <img src={azureActivityLog} alt="" className="size-10" />
                  <CardTitle className="flex items-center gap-2">
                    <Trans>SOC and ISO Reports</Trans>
                    <ExternalLinkIcon className="size-4 text-muted-foreground" />
                  </CardTitle>
                  <CardDescription>
                    <Trans>Audit reports and assessments</Trans>
                  </CardDescription>
                </Card>
              </Link>
              <Link
                href="https://www.microsoft.com/licensing/docs/view/Microsoft-Products-and-Services-Data-Protection-Addendum-DPA"
                target="_blank"
                rel="noopener noreferrer"
                aria-label={t`Microsoft DPA (opens in new window)`}
                underline={false}
                className="block min-w-0 whitespace-normal rounded-xl outline-ring focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2"
              >
                <Card className="h-full gap-3 px-6 py-5 transition-colors hover:bg-hover-background">
                  <img src={azurePolicy} alt="" className="size-10" />
                  <CardTitle className="flex items-center gap-2">
                    <Trans>Microsoft DPA</Trans>
                    <ExternalLinkIcon className="size-4 text-muted-foreground" />
                  </CardTitle>
                  <CardDescription>
                    <Trans>Data protection addendum</Trans>
                  </CardDescription>
                </Card>
              </Link>
            </div>
          </div>
        </section>
      </div>
      <PublicFooter />
    </main>
  );
}
