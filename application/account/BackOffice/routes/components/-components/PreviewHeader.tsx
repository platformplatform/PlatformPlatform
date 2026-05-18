import { Trans } from "@lingui/react/macro";
import {
  Breadcrumb,
  BreadcrumbItem,
  BreadcrumbLink,
  BreadcrumbList,
  BreadcrumbPage
} from "@repo/ui/components/Breadcrumb";
import { Link } from "@repo/ui/components/Link";
import { useEffect, useState } from "react";

type PreviewHeaderProps = Readonly<{
  currentPage: "components" | "examples" | "charts" | "emails";
  tabLabels: Record<string, React.ReactNode>;
  defaultTab: string;
  rightContent?: React.ReactNode;
}>;

const sectionConfig = {
  components: { href: "/components", label: <Trans>Components</Trans> },
  examples: { href: "/components/examples", label: <Trans>Examples</Trans> },
  charts: { href: "/components/charts", label: <Trans>Charts</Trans> },
  emails: { href: "/components/emails", label: <Trans>Emails</Trans> }
} as const;

export function PreviewHeader({ currentPage, tabLabels, defaultTab, rightContent }: PreviewHeaderProps) {
  const [activeTab, setActiveTab] = useState(() => window.location.hash.replace("#", "") || defaultTab);

  useEffect(() => {
    const handleHashChange = () => setActiveTab(window.location.hash.replace("#", "") || defaultTab);
    window.addEventListener("hashchange", handleHashChange);
    return () => window.removeEventListener("hashchange", handleHashChange);
  }, [defaultTab]);

  const activeLabel = tabLabels[activeTab];
  const { href: sectionHref, label: sectionLabel } = sectionConfig[currentPage];

  return (
    <nav className="hidden w-full justify-between gap-2 sm:flex">
      <Breadcrumb>
        <BreadcrumbList>
          <BreadcrumbItem>
            <BreadcrumbLink render={<Link href="/" variant="secondary" underline={false} />}>
              <Trans>Home</Trans>
            </BreadcrumbLink>
          </BreadcrumbItem>
          <BreadcrumbItem>
            {activeLabel ? (
              <BreadcrumbLink
                render={
                  <Link
                    href={sectionHref}
                    variant="secondary"
                    underline={false}
                    onClick={() => {
                      window.location.hash = "";
                    }}
                  />
                }
              >
                {sectionLabel}
              </BreadcrumbLink>
            ) : (
              <BreadcrumbPage>{sectionLabel}</BreadcrumbPage>
            )}
          </BreadcrumbItem>
          {activeLabel && activeLabel !== sectionLabel && (
            <BreadcrumbItem>
              <BreadcrumbPage>{activeLabel}</BreadcrumbPage>
            </BreadcrumbItem>
          )}
        </BreadcrumbList>
      </Breadcrumb>
      {rightContent && <span className="flex items-center gap-2">{rightContent}</span>}
    </nav>
  );
}
