import { Trans } from "@lingui/react/macro";
import { TableHead, TableHeader, TableRow } from "@repo/ui/components/Table";
import { ArrowUp } from "lucide-react";

import { SortableUserProperties } from "@/shared/lib/api/sortTypes";

export type SortDescriptor = {
  column: string;
  direction: "ascending" | "descending";
};

interface UserTableHeaderProps {
  sortDescriptor: SortDescriptor;
  isMobile: boolean;
  onSortChange: (columnId: string) => void;
}

export function UserTableHeader({ sortDescriptor, isMobile, onSortChange }: Readonly<UserTableHeaderProps>) {
  return (
    <TableHeader className="z-10 bg-inherit sm:sticky sm:top-0">
      <TableRow>
        <TableHead data-column={SortableUserProperties.Name} onClick={() => onSortChange(SortableUserProperties.Name)}>
          <Trans>Name</Trans>
          <SortIndicator sortDescriptor={sortDescriptor} columnId={SortableUserProperties.Name} />
        </TableHead>
        {!isMobile && (
          <>
            <TableHead
              data-column={SortableUserProperties.Email}
              onClick={() => onSortChange(SortableUserProperties.Email)}
            >
              <Trans>Email</Trans>
              <SortIndicator sortDescriptor={sortDescriptor} columnId={SortableUserProperties.Email} />
            </TableHead>
            <TableHead
              data-column={SortableUserProperties.CreatedAt}
              className="w-[7rem] min-w-[4rem]"
              onClick={() => onSortChange(SortableUserProperties.CreatedAt)}
            >
              <Trans>Created</Trans>
              <SortIndicator sortDescriptor={sortDescriptor} columnId={SortableUserProperties.CreatedAt} />
            </TableHead>
            <TableHead
              data-column={SortableUserProperties.LastSeenAt}
              className="w-[7.5rem] min-w-[4rem]"
              onClick={() => onSortChange(SortableUserProperties.LastSeenAt)}
            >
              <Trans>Last seen</Trans>
              <SortIndicator sortDescriptor={sortDescriptor} columnId={SortableUserProperties.LastSeenAt} />
            </TableHead>
            <TableHead
              data-column={SortableUserProperties.Role}
              className="w-[8.5rem]"
              onClick={() => onSortChange(SortableUserProperties.Role)}
            >
              <Trans>Role</Trans>
              <SortIndicator sortDescriptor={sortDescriptor} columnId={SortableUserProperties.Role} />
            </TableHead>
          </>
        )}
      </TableRow>
    </TableHeader>
  );
}

interface SortIndicatorProps {
  sortDescriptor: SortDescriptor;
  columnId: string;
}

function SortIndicator({ sortDescriptor, columnId }: Readonly<SortIndicatorProps>) {
  if (sortDescriptor.column !== columnId) {
    return null;
  }

  return (
    <span
      className={`flex size-4 items-center justify-center transition ${sortDescriptor.direction === "descending" ? "rotate-180" : ""}`}
    >
      <ArrowUp aria-hidden={true} className="size-4 text-muted-foreground" />
    </span>
  );
}
