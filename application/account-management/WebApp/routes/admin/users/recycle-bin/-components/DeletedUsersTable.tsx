import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Avatar } from "@repo/ui/components/Avatar";
import { Badge } from "@repo/ui/components/Badge";
import { Pagination } from "@repo/ui/components/Pagination";
import { Cell, Column, Row, Table, TableHeader } from "@repo/ui/components/Table";
import { Text } from "@repo/ui/components/Text";
import { useViewportResize } from "@repo/ui/hooks/useViewportResize";
import { isMediumViewportOrLarger, isSmallViewportOrLarger, isTouchDevice } from "@repo/ui/utils/responsive";
import { getInitials } from "@repo/utils/string/getInitials";
import { useCallback } from "react";
import type { Selection } from "react-aria-components";
import { TableBody } from "react-aria-components";
import { SmartDate } from "@/shared/components/SmartDate";
import { api, type components } from "@/shared/lib/api/client";
import { getUserRoleLabel } from "@/shared/lib/api/userRole";

type DeletedUserDetails = components["schemas"]["DeletedUserDetails"];

interface DeletedUsersTableProps {
  selectedUsers: DeletedUserDetails[];
  onSelectedUsersChange: (users: DeletedUserDetails[]) => void;
  pageOffset: number;
  onPageChange: (page: number) => void;
}

export function DeletedUsersTable({
  selectedUsers,
  onSelectedUsersChange,
  pageOffset,
  onPageChange
}: Readonly<DeletedUsersTableProps>) {
  const isMobile = useViewportResize();

  const { data: deletedUsersData, isLoading } = api.useQuery("get", "/api/account-management/users/deleted", {
    params: {
      query: {
        PageOffset: pageOffset,
        PageSize: 25
      }
    }
  });

  const handleSelectionChange = useCallback(
    (keys: Selection) => {
      if (keys === "all") {
        onSelectedUsersChange(deletedUsersData?.users ?? []);
        return;
      }

      const selectedUsersList = deletedUsersData?.users.filter((user) => keys.has(user.id)) ?? [];
      onSelectedUsersChange(selectedUsersList);
    },
    [deletedUsersData?.users, onSelectedUsersChange]
  );

  if (isLoading) {
    return null;
  }

  const users = deletedUsersData?.users ?? [];
  const currentPage = (deletedUsersData?.currentPageOffset ?? 0) + 1;
  const totalPages = deletedUsersData?.totalPages ?? 1;

  if (users.length === 0) {
    return (
      <div className="flex flex-1 items-center justify-center py-16">
        <Text className="text-muted-foreground">
          <Trans>No deleted users</Trans>
        </Text>
      </div>
    );
  }

  return (
    <>
      <div className="deleted-users-table min-h-48 flex-1">
        <Table
          key={pageOffset}
          selectionMode={isTouchDevice() || !isMediumViewportOrLarger() ? "single" : "multiple"}
          selectionBehavior="replace"
          selectedKeys={new Set(selectedUsers.map((user) => user.id))}
          onSelectionChange={handleSelectionChange}
          aria-label={t`Deleted users`}
          className={isMobile ? "[&>div>div>div]:-webkit-overflow-scrolling-touch" : ""}
        >
          <TableHeader>
            <Column isRowHeader={true} minWidth={isSmallViewportOrLarger() ? 250 : undefined}>
              <Trans>Name</Trans>
            </Column>
            {isSmallViewportOrLarger() && (
              <Column minWidth={160}>
                <Trans>Email</Trans>
              </Column>
            )}
            {isMediumViewportOrLarger() && (
              <Column minWidth={120} defaultWidth={140}>
                <Trans>Deleted</Trans>
              </Column>
            )}
            {isSmallViewportOrLarger() && (
              <Column width={100}>
                <Trans>Role</Trans>
              </Column>
            )}
          </TableHeader>
          <TableBody>
            {users.map((user) => (
              <Row key={user.id} id={user.id}>
                <Cell>
                  <div className="flex h-14 w-full items-center justify-between gap-2 p-0">
                    <div className="flex min-w-0 flex-1 items-center gap-2 text-left font-normal">
                      <Avatar
                        initials={getInitials(user.firstName, user.lastName, user.email)}
                        avatarUrl={user.avatarUrl}
                        size="sm"
                        isRound={true}
                      />
                      <div className="flex min-w-0 flex-1 flex-col">
                        <div className="flex items-center gap-2 truncate text-foreground">
                          <span className="truncate">
                            {user.firstName || user.lastName
                              ? `${user.firstName} ${user.lastName}`.trim()
                              : !isSmallViewportOrLarger()
                                ? user.email
                                : ""}
                          </span>
                        </div>
                        <span className="block truncate text-muted-foreground text-sm">{user.title ?? ""}</span>
                      </div>
                    </div>
                  </div>
                </Cell>
                {isSmallViewportOrLarger() && (
                  <Cell>
                    <Text className="h-full w-full justify-start p-0 text-left font-normal">{user.email}</Text>
                  </Cell>
                )}
                {isMediumViewportOrLarger() && (
                  <Cell>
                    <SmartDate date={user.deletedAt} className="text-foreground" />
                  </Cell>
                )}
                {isSmallViewportOrLarger() && (
                  <Cell>
                    <Badge variant="outline">{getUserRoleLabel(user.role)}</Badge>
                  </Cell>
                )}
              </Row>
            ))}
          </TableBody>
        </Table>
      </div>

      {!isMobile && totalPages > 1 && (
        <div className="flex-shrink-0 bg-background pt-4">
          <Pagination
            paginationSize={9}
            currentPage={currentPage}
            totalPages={totalPages}
            onPageChange={onPageChange}
            previousLabel={t`Previous`}
            nextLabel={t`Next`}
            className="w-full"
          />
        </div>
      )}
    </>
  );
}
