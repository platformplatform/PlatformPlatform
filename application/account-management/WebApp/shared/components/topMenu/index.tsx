import { Trans } from "@lingui/react/macro";
import {
  Breadcrumb,
  BreadcrumbItem,
  BreadcrumbLink,
  BreadcrumbList,
  BreadcrumbSeparator
} from "@repo/ui/components/Breadcrumb";
import { Link } from "@repo/ui/components/Link";
import { Children, type ReactNode } from "react";
import FederatedTopMenu from "@/federated-modules/topMenu/FederatedTopMenu";

interface TopMenuProps {
  children?: ReactNode;
}

export function TopMenu({ children }: Readonly<TopMenuProps>) {
  const childArray = Children.toArray(children);
  const lastIndex = childArray.length - 1;

  return (
    <FederatedTopMenu>
      <Breadcrumb>
        <BreadcrumbList>
          <BreadcrumbItem>
            <BreadcrumbLink render={<Link href="/admin" variant="secondary" underline={false} />}>
              <Trans>Home</Trans>
            </BreadcrumbLink>
          </BreadcrumbItem>
          {childArray.map((child, index) => (
            <span key={index} className="contents">
              <BreadcrumbSeparator />
              {index === lastIndex ? <BreadcrumbItem>{child}</BreadcrumbItem> : child}
            </span>
          ))}
        </BreadcrumbList>
      </Breadcrumb>
    </FederatedTopMenu>
  );
}
