import { Trans } from "@lingui/react/macro";
import {
  Breadcrumb,
  BreadcrumbItem,
  BreadcrumbList,
  BreadcrumbPage,
  BreadcrumbSeparator
} from "@repo/ui/components/Breadcrumb";
import { Children, lazy, type ReactNode, Suspense } from "react";

const FederatedTopMenu = lazy(() => import("account-management/FederatedTopMenu"));

interface TopMenuProps {
  children?: ReactNode;
}

export function TopMenu({ children }: Readonly<TopMenuProps>) {
  const childArray = Children.toArray(children);
  const lastIndex = childArray.length - 1;

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
            {childArray.map((child, index) => (
              <span key={index} className="contents">
                <BreadcrumbSeparator />
                {index === lastIndex ? <BreadcrumbItem>{child}</BreadcrumbItem> : child}
              </span>
            ))}
          </BreadcrumbList>
        </Breadcrumb>
      </FederatedTopMenu>
    </Suspense>
  );
}
