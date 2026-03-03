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
        <TableHead
          data-column={SortableUserProperties.Name}
          className="cursor-pointer select-none"
          onClick={() => onSortChange(SortableUserProperties.Name)}
        >
          <div className="flex items-center gap-1 text-xs font-bold">
            <span>
              <Trans>Name</Trans>
            </span>
            <SortIndicator sortDescriptor={sortDescriptor} columnId={SortableUserProperties.Name} />
          </div>
        </TableHead>
        {!isMobile && (
          <>
            <TableHead
              data-column={SortableUserProperties.Email}
              className="cursor-pointer select-none"
              onClick={() => onSortChange(SortableUserProperties.Email)}
            >
              <div className="flex items-center gap-1 text-xs font-bold">
                <span>
                  <Trans>Email</Trans>
                </span>
                <SortIndicator sortDescriptor={sortDescriptor} columnId={SortableUserProperties.Email} />
              </div>
            </TableHead>
            <TableHead
              data-column={SortableUserProperties.CreatedAt}
              className="w-[7rem] min-w-[4rem] cursor-pointer select-none"
              onClick={() => onSortChange(SortableUserProperties.CreatedAt)}
            >
              <div className="flex items-center gap-1 text-xs font-bold">
                <span>
                  <Trans>Created</Trans>
                </span>
                <SortIndicator sortDescriptor={sortDescriptor} columnId={SortableUserProperties.CreatedAt} />
              </div>
            </TableHead>
            <TableHead
              data-column={SortableUserProperties.LastSeenAt}
              className="w-[7.5rem] min-w-[4rem] cursor-pointer select-none"
              onClick={() => onSortChange(SortableUserProperties.LastSeenAt)}
            >
              <div className="flex items-center gap-1 text-xs font-bold">
                <span>
                  <Trans>Last seen</Trans>
                </span>
                <SortIndicator sortDescriptor={sortDescriptor} columnId={SortableUserProperties.LastSeenAt} />
              </div>
            </TableHead>
            <TableHead
              data-column={SortableUserProperties.Role}
              className="w-[8.5rem] cursor-pointer select-none"
              onClick={() => onSortChange(SortableUserProperties.Role)}
            >
              <div className="flex items-center gap-1 text-xs font-bold">
                <span>
                  <Trans>Role</Trans>
                </span>
                <SortIndicator sortDescriptor={sortDescriptor} columnId={SortableUserProperties.Role} />
              </div>
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
