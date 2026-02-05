import { Trans } from "@lingui/react/macro";
import { Breadcrumb, BreadcrumbItem, BreadcrumbList, BreadcrumbPage } from "@repo/ui/components/Breadcrumb";
import { lazy, type ReactNode, Suspense } from "react";

const FederatedTopMenu = lazy(() => import("account-management/FederatedTopMenu"));

interface TopMenuProps {
  children?: ReactNode;
}

export function TopMenu({ children }: Readonly<TopMenuProps>) {
  return (
    <Suspense fallback={<div className="h-12 w-full" />}>
      <FederatedTopMenu>
        <Breadcrumb>
          <BreadcrumbList>
            <BreadcrumbItem>
              <BreadcrumbPage>
                <Trans>Home</Trans>
              </BreadcrumbPage>
            </BreadcrumbItem>
            {children}
          </BreadcrumbList>
        </Breadcrumb>
      </FederatedTopMenu>
    </Suspense>
  );
}
