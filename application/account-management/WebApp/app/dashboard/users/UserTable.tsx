import { EllipsisVerticalIcon, TrashIcon } from "lucide-react";
import type { SortDescriptor } from "react-aria-components";
import { TableBody } from "react-aria-components";
import { useMemo, useState } from "react";
import { rows } from "./data";
import { Cell, Column, Row, Table, TableHeader } from "@/ui/components/Table";
import Badge from "@/ui/components/Badge";
import Pagination from "@/ui/components/Pagination";

export function UserTable() {
  const [sortDescriptor, setSortDescriptor] = useState<SortDescriptor>({
    column: "name",
    direction: "ascending",
  });
  const [currentPage, setCurrentPage] = useState(1);
  const itemsPerPage = 10;

  const sortedRows = useMemo(() => {
    // eslint-disable-next-line ts/ban-ts-comment
    // @ts-expect-error
    const items = rows.slice().sort((a, b) => a[sortDescriptor.column].localeCompare(b[sortDescriptor.column]));
    if (sortDescriptor.direction === "descending")
      items.reverse();

    return items;
  }, [sortDescriptor]);

  const paginatedRows = useMemo(() => {
    const startIndex = (currentPage - 1) * itemsPerPage;
    const endIndex = startIndex + itemsPerPage;
    return sortedRows.slice(startIndex, endIndex);
  }, [sortedRows, currentPage]);

  return (
    <div>
      <div className="mb-4 w-full min-w-64 overflow-x-auto">
        <Table selectionMode="multiple" sortDescriptor={sortDescriptor} onSortChange={setSortDescriptor}>
          <TableHeader>
            <Column minWidth={100} allowsSorting id="name" isRowHeader>
              Name
            </Column>
            <Column minWidth={100} allowsSorting id="email">
              Email
            </Column>
            <Column defaultWidth={130} allowsSorting id="date">
              Added
            </Column>
            <Column defaultWidth={130} allowsSorting id="lastSeen">
              Last Seen
            </Column>
            <Column defaultWidth={100} allowsSorting id="type">
              Role
            </Column>
            <Column width={80}>
              Actions
            </Column>
          </TableHeader>
          <TableBody>
            {paginatedRows.map(user => (
              <Row key={user.email}>
                <Cell>
                  <div className="flex h-14 items-center">
                    {user.profilePicture
                      ? (
                        <img src={user.profilePicture} alt="User avatar" className="mr-2 w-10 h-10 rounded-full bg-transparent" />
                        )
                      : (
                        <div className="w-10 h-10 min-w-[2.5rem] min-h-[2.5rem] rounded-full bg-gray-200 mr-2 flex items-center justify-center text-sm font-semibold uppercase">AB</div>
                        )}
                    <div className="truncate">
                      {user.name}
                      <br />
                      <span className="text-gray-500">{user.title}</span>
                    </div>
                  </div>
                </Cell>
                <Cell><span className="text-gray-500">{user.email}</span></Cell>
                <Cell><span className="text-gray-500">{user.added.toLocaleDateString(undefined, { day: "numeric", month: "short", year: "numeric" })}</span></Cell>
                <Cell><span className="text-gray-500">{user.lastSeen.toLocaleDateString(undefined, { day: "numeric", month: "short", year: "numeric" })}</span></Cell>
                <Cell><Badge>{user.role}</Badge></Cell>
                <Cell>
                  <div className="flex gap-8">
                    <button type="button" className="group-hover:visible invisible">
                      <TrashIcon size={16} />
                    </button>
                    <button type="button">
                      <EllipsisVerticalIcon size={16} />
                    </button>
                  </div>
                </Cell>
              </Row>
            ))}
          </TableBody>
        </Table>
      </div>
      <Pagination
        total={rows.length}
        itemsPerPage={itemsPerPage}
        currentPage={currentPage}
        onPageChange={setCurrentPage}
      />
    </div>
  );
}
