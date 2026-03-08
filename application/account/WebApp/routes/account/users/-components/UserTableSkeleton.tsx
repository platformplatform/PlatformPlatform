import { t } from "@lingui/core/macro";
import { Skeleton } from "@repo/ui/components/Skeleton";
import { Table, TableBody, TableCell, TableRow } from "@repo/ui/components/Table";

interface UserTableSkeletonProps {
  isMobile: boolean;
}

export function UserTableSkeleton({ isMobile }: Readonly<UserTableSkeletonProps>) {
  return (
    <div className="min-h-48 flex-1 overflow-auto rounded-md">
      <Table aria-label={t`Users loading`}>
        <TableBody>
          <TableRow className="h-10">
            <TableCell>
              <Skeleton className="h-3 w-12" />
            </TableCell>
            {!isMobile && (
              <>
                <TableCell>
                  <Skeleton className="h-3 w-12" />
                </TableCell>
                <TableCell>
                  <Skeleton className="h-3 w-16" />
                </TableCell>
                <TableCell>
                  <Skeleton className="h-3 w-16" />
                </TableCell>
                <TableCell>
                  <Skeleton className="h-3 w-10" />
                </TableCell>
              </>
            )}
          </TableRow>
          {Array.from({ length: 5 }).map((_, index) => (
            <TableRow key={index}>
              <TableCell>
                <div className="flex h-14 items-center gap-2">
                  <Skeleton className="size-10 rounded-full" />
                  <div className="flex flex-col gap-1">
                    <Skeleton className="h-4 w-32" />
                    <Skeleton className="h-3 w-24" />
                  </div>
                </div>
              </TableCell>
              {!isMobile && (
                <>
                  <TableCell>
                    <Skeleton className="h-4 w-40" />
                  </TableCell>
                  <TableCell>
                    <Skeleton className="h-4 w-16" />
                  </TableCell>
                  <TableCell>
                    <Skeleton className="h-4 w-16" />
                  </TableCell>
                  <TableCell>
                    <Skeleton className="h-5 w-16 rounded-full" />
                  </TableCell>
                </>
              )}
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </div>
  );
}
