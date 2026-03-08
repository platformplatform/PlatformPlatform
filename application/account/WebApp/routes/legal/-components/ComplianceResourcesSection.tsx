import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Card, CardDescription, CardTitle } from "@repo/ui/components/Card";
import { Link } from "@repo/ui/components/Link";
import { ExternalLinkIcon } from "lucide-react";

import azureActivityLog from "@/shared/images/icons/azure-activity-log.svg";
import azureCompliance from "@/shared/images/icons/azure-compliance.svg";
import azurePolicy from "@/shared/images/icons/azure-policy.svg";
import azureSecurity from "@/shared/images/icons/azure-security.svg";

export function ComplianceResourcesSection() {
  return (
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
            className="block min-w-0 rounded-xl whitespace-normal outline-ring focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2"
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
            className="block min-w-0 rounded-xl whitespace-normal outline-ring focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2"
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
            className="block min-w-0 rounded-xl whitespace-normal outline-ring focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2"
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
            className="block min-w-0 rounded-xl whitespace-normal outline-ring focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2"
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
  );
}
