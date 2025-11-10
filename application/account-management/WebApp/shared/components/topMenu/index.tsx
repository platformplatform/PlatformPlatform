import { Trans } from "@lingui/react/macro";
import { Breadcrumb, Breadcrumbs } from "@repo/ui/components/Breadcrumbs";
import type { ReactNode } from "react";
import FederatedTopMenu from "@/federated-modules/topMenu/FederatedTopMenu";

interface TopMenuProps {
  children?: ReactNode;
}

export function TopMenu({ children }: Readonly<TopMenuProps>) {
  return (
    <FederatedTopMenu>
      <Breadcrumbs>
        <Breadcrumb href="/admin">
          <Trans>Home</Trans>
        </Breadcrumb>
        {children}
      </Breadcrumbs>
    </FederatedTopMenu>
  );
}
