import { Trans } from "@lingui/react/macro";
import { Image } from "@repo/ui/components/Image";
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
              <h1 className="mb-3 font-bold text-4xl text-foreground md:text-5xl">
                <Trans>Legal and Compliance</Trans>
              </h1>
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
                className="flex flex-col whitespace-normal rounded-xl bg-input-background p-6 transition-colors hover:bg-hover-background focus-visible:outline focus-visible:outline-2 focus-visible:outline-ring"
              >
                <ScrollTextIcon className="mb-4 h-10 w-10 text-primary" />
                <h3 className="mb-2 font-semibold text-foreground text-lg">
                  <Trans>Terms of Service</Trans>
                </h3>
                <p className="text-muted-foreground text-sm">
                  <Trans>
                    The agreement governing your use of our Service, including acceptable use and liability.
                  </Trans>
                </p>
              </Link>

              <Link
                href="/legal/privacy"
                underline={false}
                className="flex flex-col whitespace-normal rounded-xl bg-input-background p-6 transition-colors hover:bg-hover-background focus-visible:outline focus-visible:outline-2 focus-visible:outline-ring"
              >
                <ShieldCheckIcon className="mb-4 h-10 w-10 text-primary" />
                <h3 className="mb-2 font-semibold text-foreground text-lg">
                  <Trans>Privacy Policy</Trans>
                </h3>
                <p className="text-muted-foreground text-sm">
                  <Trans>How we collect, use, and protect your personal data in compliance with GDPR.</Trans>
                </p>
              </Link>

              <Link
                href="/legal/dpa"
                underline={false}
                className="flex flex-col whitespace-normal rounded-xl bg-input-background p-6 transition-colors hover:bg-hover-background focus-visible:outline focus-visible:outline-2 focus-visible:outline-ring"
              >
                <FileTextIcon className="mb-4 h-10 w-10 text-primary" />
                <h3 className="mb-2 font-semibold text-foreground text-lg">
                  <Trans>Data Processing Agreement</Trans>
                </h3>
                <p className="text-muted-foreground text-sm">
                  <Trans>GDPR Article 28 compliant agreement for processing data on your behalf.</Trans>
                </p>
              </Link>
            </div>
          </div>
        </section>

        {/* Section 2: Enterprise-grade Azure Infrastructure */}
        <section className="bg-input-background px-6 py-16">
          <div className="mx-auto max-w-5xl">
            {/* Header */}
            <div className="mb-12 text-center">
              <h2 className="mb-3 font-bold text-4xl text-foreground md:text-5xl">
                <Trans>Enterprise-grade Azure infrastructure</Trans>
              </h2>
              <p className="text-muted-foreground">
                <Trans>Microsoft Azure Platform-as-a-Service (PaaS) with enterprise-grade reliability.</Trans>
              </p>
            </div>

            {/* Compliance Badges */}
            <div className="mb-12 flex flex-wrap items-center justify-center gap-6 sm:gap-8 md:gap-12">
              <div className="flex flex-col items-center gap-3">
                <div className="flex h-24 w-24 items-center justify-center">
                  <img src={gdprBadge} alt="GDPR Compliant" className="h-20 w-auto object-contain" />
                </div>
                <span className="font-semibold text-foreground">
                  <Trans>GDPR Compliant</Trans>
                </span>
              </div>
              <div className="flex flex-col items-center gap-3">
                <div className="flex h-24 w-24 items-center justify-center rounded-full bg-[#0078d4]/10">
                  <svg viewBox="0 0 96 96" className="h-14 w-14" fill="none" xmlns="http://www.w3.org/2000/svg">
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
              <div className="rounded-xl bg-background p-6 transition-colors hover:bg-hover-background dark:bg-card">
                <div className="mb-3 flex h-10 w-10 items-center justify-center rounded-lg bg-primary/10">
                  <ServerIcon className="h-6 w-6 text-primary" />
                </div>
                <h3 className="mb-1 font-semibold text-foreground">
                  <Trans>Fully managed infrastructure</Trans>
                </h3>
                <p className="text-muted-foreground text-sm">
                  <Trans>Microsoft patches and secures all PaaS infrastructure automatically</Trans>
                </p>
              </div>
              <div className="rounded-xl bg-background p-6 transition-colors hover:bg-hover-background dark:bg-card">
                <div className="mb-3 flex h-10 w-10 items-center justify-center rounded-lg bg-primary/10">
                  <FingerprintIcon className="h-6 w-6 text-primary" />
                </div>
                <h3 className="mb-1 font-semibold text-foreground">
                  <Trans>Managed identities</Trans>
                </h3>
                <p className="text-muted-foreground text-sm">
                  <Trans>Only trusted personnel access data through Azure AD authentication</Trans>
                </p>
              </div>
              <div className="rounded-xl bg-background p-6 transition-colors hover:bg-hover-background dark:bg-card">
                <div className="mb-3 flex h-10 w-10 items-center justify-center rounded-lg bg-primary/10">
                  <LayersIcon className="h-6 w-6 text-primary" />
                </div>
                <h3 className="mb-1 font-semibold text-foreground">
                  <Trans>Environment isolation</Trans>
                </h3>
                <p className="text-muted-foreground text-sm">
                  <Trans>Strict separation prevents production data from leaking to other environments</Trans>
                </p>
              </div>
              <div className="rounded-xl bg-background p-6 transition-colors hover:bg-hover-background dark:bg-card">
                <div className="mb-3 flex h-10 w-10 items-center justify-center rounded-lg bg-primary/10">
                  <KeyIcon className="h-6 w-6 text-primary" />
                </div>
                <h3 className="mb-1 font-semibold text-foreground">
                  <Trans>Passwordless deployments</Trans>
                </h3>
                <p className="text-muted-foreground text-sm">
                  <Trans>GitHub deploys directly to Azure without passwords or API keys</Trans>
                </p>
              </div>
              <div className="rounded-xl bg-background p-6 transition-colors hover:bg-hover-background dark:bg-card">
                <div className="mb-3 flex h-10 w-10 items-center justify-center rounded-lg bg-primary/10">
                  <DatabaseIcon className="h-6 w-6 text-primary" />
                </div>
                <h3 className="mb-1 font-semibold text-foreground">
                  <Trans>Data residency</Trans>
                </h3>
                <p className="text-muted-foreground text-sm">
                  <Trans>Your data stays in the Azure region you select at signup</Trans>
                </p>
              </div>
              <div className="rounded-xl bg-background p-6 transition-colors hover:bg-hover-background dark:bg-card">
                <div className="mb-3 flex h-10 w-10 items-center justify-center rounded-lg bg-primary/10">
                  <ShieldCheckIcon className="h-6 w-6 text-primary" />
                </div>
                <h3 className="mb-1 font-semibold text-foreground">
                  <Trans>100% Security Score</Trans>
                </h3>
                <p className="text-muted-foreground text-sm">
                  <Trans>Achieved in Microsoft Defender for Cloud following Azure best practices</Trans>
                </p>
              </div>
            </div>

            {/* PlatformPlatform Credit */}
            <div className="flex items-center justify-center gap-3">
              <Image src={platformLogo} alt="PlatformPlatform" className="h-8 w-8" width={32} height={32} />
              <p className="text-muted-foreground text-sm">
                <Trans>
                  Built on{" "}
                  <Link
                    href="https://github.com/platformplatform/PlatformPlatform"
                    target="_blank"
                    rel="noopener noreferrer"
                    aria-label="PlatformPlatform on GitHub (opens in new window)"
                    className="inline-flex items-center gap-1 font-medium text-primary"
                  >
                    PlatformPlatform
                    <ExternalLinkIcon className="h-3 w-3" />
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
              <h2 className="mb-3 font-bold text-4xl text-foreground md:text-5xl">
                <Trans>Microsoft compliance resources</Trans>
              </h2>
              <p className="text-muted-foreground">
                <Trans>
                  For detailed compliance certifications and audit reports, visit Microsoft's official resources.
                </Trans>
              </p>
            </div>
            <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
              <Link
                href="https://www.microsoft.com/en-us/trust-center"
                target="_blank"
                rel="noopener noreferrer"
                aria-label="Trust Center (opens in new window)"
                underline={false}
                className="flex flex-col whitespace-normal rounded-xl bg-input-background p-5 transition-colors hover:bg-hover-background focus-visible:outline focus-visible:outline-2 focus-visible:outline-ring"
              >
                <div className="mb-3">
                  <img src={azureSecurity} alt="" className="h-10 w-10" />
                </div>
                <div className="flex items-center gap-2">
                  <span className="font-semibold text-foreground">
                    <Trans>Trust Center</Trans>
                  </span>
                  <ExternalLinkIcon className="h-4 w-4 text-muted-foreground" />
                </div>
                <p className="mt-1 text-muted-foreground text-sm">
                  <Trans>Security, privacy, and compliance information</Trans>
                </p>
              </Link>
              <Link
                href="https://learn.microsoft.com/en-us/azure/compliance/"
                target="_blank"
                rel="noopener noreferrer"
                aria-label="Azure Compliance (opens in new window)"
                underline={false}
                className="flex flex-col whitespace-normal rounded-xl bg-input-background p-5 transition-colors hover:bg-hover-background focus-visible:outline focus-visible:outline-2 focus-visible:outline-ring"
              >
                <div className="mb-3">
                  <img src={azureCompliance} alt="" className="h-10 w-10" />
                </div>
                <div className="flex items-center gap-2">
                  <span className="font-semibold text-foreground">
                    <Trans>Azure Compliance</Trans>
                  </span>
                  <ExternalLinkIcon className="h-4 w-4 text-muted-foreground" />
                </div>
                <p className="mt-1 text-muted-foreground text-sm">
                  <Trans>Regulatory standards and certifications</Trans>
                </p>
              </Link>
              <Link
                href="https://servicetrust.microsoft.com/"
                target="_blank"
                rel="noopener noreferrer"
                aria-label="SOC and ISO Reports (opens in new window)"
                underline={false}
                className="flex flex-col whitespace-normal rounded-xl bg-input-background p-5 transition-colors hover:bg-hover-background focus-visible:outline focus-visible:outline-2 focus-visible:outline-ring"
              >
                <div className="mb-3">
                  <img src={azureActivityLog} alt="" className="h-10 w-10" />
                </div>
                <div className="flex items-center gap-2">
                  <span className="font-semibold text-foreground">
                    <Trans>SOC and ISO Reports</Trans>
                  </span>
                  <ExternalLinkIcon className="h-4 w-4 text-muted-foreground" />
                </div>
                <p className="mt-1 text-muted-foreground text-sm">
                  <Trans>Audit reports and assessments</Trans>
                </p>
              </Link>
              <Link
                href="https://www.microsoft.com/licensing/docs/view/Microsoft-Products-and-Services-Data-Protection-Addendum-DPA"
                target="_blank"
                rel="noopener noreferrer"
                aria-label="Microsoft DPA (opens in new window)"
                underline={false}
                className="flex flex-col whitespace-normal rounded-xl bg-input-background p-5 transition-colors hover:bg-hover-background focus-visible:outline focus-visible:outline-2 focus-visible:outline-ring"
              >
                <div className="mb-3">
                  <img src={azurePolicy} alt="" className="h-10 w-10" />
                </div>
                <div className="flex items-center gap-2">
                  <span className="font-semibold text-foreground">
                    <Trans>Microsoft DPA</Trans>
                  </span>
                  <ExternalLinkIcon className="h-4 w-4 text-muted-foreground" />
                </div>
                <p className="mt-1 text-muted-foreground text-sm">
                  <Trans>Data protection addendum</Trans>
                </p>
              </Link>
            </div>
          </div>
        </section>
      </div>
      <PublicFooter />
    </main>
  );
}
