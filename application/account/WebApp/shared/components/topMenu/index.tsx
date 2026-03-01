import { Trans } from "@lingui/react/macro";
import { Breadcrumb, BreadcrumbItem, BreadcrumbLink, BreadcrumbList } from "@repo/ui/components/Breadcrumb";
import { Link } from "@repo/ui/components/Link";
import type { ReactNode } from "react";
import FederatedTopMenu from "@/federated-modules/topMenu/FederatedTopMenu";

interface TopMenuProps {
  children?: ReactNode;
}

export function TopMenu({ children }: Readonly<TopMenuProps>) {
  return (
    <FederatedTopMenu>
      <Breadcrumb>
        <BreadcrumbList>
          <BreadcrumbItem>
            <BreadcrumbLink render={<Link href="/account" variant="secondary" underline={false} />}>
              <Trans>Home</Trans>
            </BreadcrumbLink>
          </BreadcrumbItem>
          {children}
        </BreadcrumbList>
      </Breadcrumb>
    </FederatedTopMenu>
  );
}
