import { Trans } from "@lingui/react/macro";
import { Breadcrumb, Breadcrumbs } from "@repo/ui/components/Breadcrumbs";
import type { ReactNode } from "react";
import { Suspense, lazy } from "react";

const FederatedTopMenu = lazy(() => import("account-management/FederatedTopMenu"));

interface TopMenuProps {
  children?: ReactNode;
}

export function TopMenu({ children }: Readonly<TopMenuProps>) {
  return (
    <Suspense fallback={<div className="h-12 w-full" />}>
      <FederatedTopMenu>
        <Breadcrumbs>
          <Breadcrumb>
            <Trans>Home</Trans>
          </Breadcrumb>
          {children}
        </Breadcrumbs>
      </FederatedTopMenu>
    </Suspense>
  );
}
