import { EllipsisVerticalIcon, TrashIcon, UserIcon } from "lucide-react";
import type { SortDescriptor } from "react-aria-components";
import { MenuTrigger, TableBody } from "react-aria-components";
import { use, useMemo, useState } from "react";
import { Cell, Column, Row, Table, TableHeader } from "@repo/ui/components/Table";
import Badge from "@repo/ui/components/Badge";
import Pagination from "@repo/ui/components/Pagination";
import { Popover } from "@repo/ui/components/Popover";
import { Menu, MenuItem, MenuSeparator } from "@repo/ui/components/Menu";
import { Button } from "@repo/ui/components/Button";
import { Avatar } from "@repo/ui/components/Avatar";
import type { components } from "@/shared/lib/api/api.generated";

type UserTableProps = {
  usersPromise: Promise<components["schemas"]["GetUsersResponseDto"]>;
};
export function UserTable({ usersPromise }: UserTableProps) {
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
    <div>
      <div className="overflow-auto">
        <Table selectionMode="multiple" sortDescriptor={sortDescriptor} onSortChange={setSortDescriptor}>
          <TableHeader>
            <Column minWidth={100} allowsSorting id="name" isRowHeader>
              Name
            </Column>
            <Column minWidth={100} allowsSorting id="email">
              Email
            </Column>
            <Column defaultWidth={130} id="date">
              Added
            </Column>
            <Column defaultWidth={130} id="lastSeen">
              Last Seen
            </Column>
            <Column defaultWidth={100} id="role">
              Role
            </Column>
            <Column width={80}>Actions</Column>
          </TableHeader>
          <TableBody>
            {paginatedRows.map((user) => (
              <Row key={user.email}>
                <Cell>
                  <div className="flex h-14 items-center">
                    <Avatar firstName={user.firstName} lastName={user.lastName} avatarUrl={user.avatarUrl} />
                    <div className="truncate">
                      <div>
                        {user.firstName} {user.lastName}
                      </div>
                      <div className="text-gray-500">{user.title ?? ""}</div>
                    </div>
                  </div>
                </Cell>
                <Cell>
                  <span className="text-gray-500">{user.email}</span>
                </Cell>
                <Cell>
                  <span className="text-gray-500">{toFormattedDate(user.createdAt)}</span>
                </Cell>
                <Cell>
                  <span className="text-gray-500">{toFormattedDate(user.modifiedAt)}</span>
                </Cell>
                <Cell>
                  <Badge>{user.role}</Badge>
                </Cell>
                <Cell>
                  <div className="flex gap-2">
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
                            <span className="text-red-600">Delete</span>
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
      </div>
      <div className="sticky bottom-0 bg-gray-50 w-full py-2">
        <Pagination
          total={totalCount ?? 0}
          itemsPerPage={itemsPerPage}
          currentPage={currentPage}
          onPageChange={setCurrentPage}
        />
      </div>
    </div>
  );
}

function toFormattedDate(input: string | undefined | null) {
  if (!input) return "";
  const date = new Date(input);
  return date.toLocaleDateString(undefined, { day: "numeric", month: "short", year: "numeric" });
}
