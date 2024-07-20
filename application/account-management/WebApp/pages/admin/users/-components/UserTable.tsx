import { EllipsisVerticalIcon, Trash2Icon, UserIcon } from "lucide-react";
import type { SortDescriptor } from "react-aria-components";
import { MenuTrigger, TableBody } from "react-aria-components";
import { useState } from "react";
import { Cell, Column, Row, Table, TableHeader } from "@repo/ui/components/Table";
import { Badge } from "@repo/ui/components/Badge";
import { Pagination } from "@repo/ui/components/Pagination";
import { Popover } from "@repo/ui/components/Popover";
import { Menu, MenuItem, MenuSeparator } from "@repo/ui/components/Menu";
import { Button } from "@repo/ui/components/Button";
import { Avatar } from "@repo/ui/components/Avatar";
import type { components } from "@/shared/lib/api/api.generated";

type UserTableProps = {
  usersData: components["schemas"]["GetUsersResponseDto"] | null;
  onPageChange: (page: number) => void;
};

export function UserTable({ usersData, onPageChange }: Readonly<UserTableProps>) {
  const [sortDescriptor, setSortDescriptor] = useState<SortDescriptor>({
    column: "firstName",
    direction: "ascending"
  });

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
          {(usersData?.users ?? []).map((user) => (
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
                <Badge variant="outline">{user.role}</Badge>
              </Cell>
              <Cell>
                <div className="flex gap-2 w-12">
                  <Button variant="icon" className="group-hover:visible invisible">
                    <Trash2Icon size={16} />
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
                          <Trash2Icon size={16} />
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
        pageOffset={usersData?.currentPageOffset ?? 0}
        totalPages={usersData?.totalPages ?? 1}
        onPageChange={onPageChange}
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
