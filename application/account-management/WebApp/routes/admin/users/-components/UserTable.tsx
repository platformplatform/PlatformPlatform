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
import { getPascalCase } from "@repo/utils/string/getPascalCase";
import type { components } from "@/shared/lib/api/api.generated";

type UserTableProps = {
  usersData: components["schemas"]["GetUsersResponseDto"] | null;
  onPageChange: (page: number) => void;
  onSortChange: [
    (column: components["schemas"]["SortableUserProperties"]) => void,
    (direction: components["schemas"]["SortOrder"]) => void
  ];
};

export function UserTable({ usersData, onPageChange, onSortChange }: Readonly<UserTableProps>) {
  const [sortDescriptor, setSortDescriptor] = useState<SortDescriptor>();

  return (
    <div className="flex flex-col gap-2 h-full">
      <Table
        selectionMode="multiple"
        selectionBehavior="toggle"
        sortDescriptor={sortDescriptor}
        onSortChange={(newSortDescriptor) => {
          setSortDescriptor(newSortDescriptor);
          onSortChange[0](
            getPascalCase(newSortDescriptor.column as string) as components["schemas"]["SortableUserProperties"]
          );
          onSortChange[1](getPascalCase(newSortDescriptor.direction) as components["schemas"]["SortOrder"]);
          onPageChange(0);
        }}
        aria-label="Users"
      >
        <TableHeader>
          <Column minWidth={50} defaultWidth={200} allowsSorting id="name" isRowHeader>
            Name
          </Column>
          <Column minWidth={50} allowsSorting id="email">
            Email
          </Column>
          <Column minWidth={55} allowsSorting id="createdAt">
            Added
          </Column>
          <Column minWidth={55} allowsSorting id="modifiedAt">
            Last Seen
          </Column>
          <Column minWidth={75} allowsSorting id="role">
            Role
          </Column>
          <Column minWidth={114} defaultWidth={114}>
            Actions
          </Column>
        </TableHeader>
        <TableBody>
          {(usersData?.users ?? []).map((user) => (
            <Row key={user.id}>
              <Cell>
                <div className="flex h-14 items-center gap-2">
                  <Avatar
                    initials={getInitials(user.firstName, user.lastName, user.email)}
                    avatarUrl={user.avatarUrl}
                    size="sm"
                    isRound
                  />
                  <div className="flex flex-col truncate">
                    <div className="truncate">
                      {user.firstName} {user.lastName}
                    </div>
                    <div className="text-muted-foreground truncate">{user.title ?? ""}</div>
                  </div>
                </div>
              </Cell>
              <Cell>{user.email}</Cell>
              <Cell>{toFormattedDate(user.createdAt)}</Cell>
              <Cell>{toFormattedDate(user.modifiedAt)}</Cell>
              <Cell>
                <Badge variant="outline">Member</Badge>
              </Cell>
              <Cell>
                <div className="group flex gap-2 w-full">
                  <Button
                    variant="icon"
                    className="group-hover:opacity-100 opacity-0 duration-300 transition-opacity ease-in-out"
                  >
                    <Trash2Icon className="w-4 h-4" />
                  </Button>
                  <MenuTrigger>
                    <Button variant="icon" aria-label="Menu">
                      <EllipsisVerticalIcon className="w-4 h-4" />
                    </Button>
                    <Popover>
                      <Menu>
                        <MenuItem onAction={() => alert("open")}>
                          <UserIcon className="w-4 h-4" />
                          View Profile
                        </MenuItem>
                        <MenuSeparator />
                        <MenuItem onAction={() => alert("rename")}>
                          <Trash2Icon className="w-4 h-4 text-destructive" />
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
