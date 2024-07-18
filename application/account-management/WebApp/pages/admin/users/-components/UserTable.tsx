import { EllipsisVerticalIcon, TrashIcon, UserIcon } from "lucide-react";
import type { SortDescriptor } from "react-aria-components";
import { MenuTrigger, TableBody } from "react-aria-components";
import { use, useMemo, useState } from "react";
import { Cell, Column, Row, Table, TableHeader } from "@repo/ui/components/Table";
import { Badge } from "@repo/ui/components/Badge";
import { Pagination } from "@repo/ui/components/Pagination";
import { Popover } from "@repo/ui/components/Popover";
import { Menu, MenuItem, MenuSeparator } from "@repo/ui/components/Menu";
import { Button } from "@repo/ui/components/Button";
import { Avatar } from "@repo/ui/components/Avatar";
import type { components } from "@/shared/lib/api/api.generated";

type UserTableProps = {
  usersPromise: Promise<components["schemas"]["GetUsersResponseDto"]>;
};
export function UserTable({ usersPromise }: Readonly<UserTableProps>) {
  const [sortDescriptor, setSortDescriptor] = useState<SortDescriptor>({
    column: "firstName",
    direction: "ascending"
  });
  const [currentPage, setCurrentPage] = useState(1);
  const itemsPerPage = 10;

  const { currentPageOffset, totalCount, totalPages, users } = use(usersPromise);

  const sortedRows = useMemo(() => {
    // @ts-expect-error
    const items = users.slice().sort((a, b) => a[sortDescriptor.column].localeCompare(b[sortDescriptor.column]));
    if (sortDescriptor.direction === "descending") items.reverse();

    return items;
  }, [sortDescriptor, users]);

  const paginatedRows = useMemo(() => {
    const startIndex = (currentPage - 1) * itemsPerPage;
    const endIndex = startIndex + itemsPerPage;
    return sortedRows.slice(startIndex, endIndex);
  }, [sortedRows, currentPage]);

  return (
    <div className="flex flex-col gap-2 h-full">
      <Table
        selectionMode="multiple"
        sortDescriptor={sortDescriptor}
        onSortChange={setSortDescriptor}
        aria-label="Users"
      >
        <TableHeader>
          <Column allowsSorting id="name" isRowHeader>
            Name
          </Column>
          <Column allowsSorting id="email">
            Email
          </Column>
          <Column id="date">Added</Column>
          <Column id="lastSeen">Last Seen</Column>
          <Column id="role">Role</Column>
          <Column>Actions</Column>
        </TableHeader>
        <TableBody>
          {paginatedRows.map((user) => (
            <Row key={user.email}>
              <Cell>
                <div className="flex h-14 items-center gap-2">
                  <Avatar
                    initials={getInitials(user.firstName, user.lastName, user.email)}
                    avatarUrl={user.avatarUrl}
                    size="sm"
                    isRound
                  />
                  <div className="truncate">
                    <div>
                      {user.firstName} {user.lastName}
                    </div>
                    <div className="text-muted-foreground">{user.title ?? ""}</div>
                  </div>
                </div>
              </Cell>
              <Cell>{user.email}</Cell>
              <Cell>{toFormattedDate(user.createdAt)}</Cell>
              <Cell>{toFormattedDate(user.modifiedAt)}</Cell>
              <Cell>
                <Badge variant={getRoleBadgeVariant(user.role)}>{user.role}</Badge>
              </Cell>
              <Cell>
                <div className="flex gap-2 w-12">
                  <Button variant="icon" className="group-hover:visible invisible">
                    <TrashIcon size={16} />
                  </Button>
                  <MenuTrigger>
                    <Button variant="icon" aria-label="Menu">
                      <EllipsisVerticalIcon size={16} />
                    </Button>
                    <Popover>
                      <Menu>
                        <MenuItem onAction={() => alert("open")}>
                          <UserIcon size={16} />
                          View Profile
                        </MenuItem>
                        <MenuSeparator />
                        <MenuItem onAction={() => alert("rename")}>
                          <TrashIcon size={16} />
                          <span className="text-destructive">Delete</span>
                        </MenuItem>
                      </Menu>
                    </Popover>
                  </MenuTrigger>
                </div>
              </Cell>
            </Row>
          ))}
        </TableBody>
      </Table>
      <Pagination
        total={totalCount ?? 0}
        itemsPerPage={itemsPerPage}
        currentPage={currentPage}
        onPageChange={setCurrentPage}
      />
    </div>
  );
}

function toFormattedDate(input: string | undefined | null) {
  if (!input) return "";
  const date = new Date(input);
  return date.toLocaleDateString(undefined, { day: "numeric", month: "short", year: "numeric" });
}

function getInitials(firstName: string | undefined, lastName: string | undefined, email: string | undefined) {
  if (firstName && lastName) return `${firstName[0]}${lastName[0]}`;
  if (email == null) return "";
  return email.split("@")[0].slice(0, 2).toUpperCase();
}

function getRoleBadgeVariant(role?: "Admin" | "Owner" | "Member") {
  if (role === "Admin") return "danger";
  if (role === "Owner") return "success";
  return "neutral";
}
