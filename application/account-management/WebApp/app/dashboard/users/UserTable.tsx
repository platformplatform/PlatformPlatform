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
    <div className="flex gap-4 flex-col">
      <Table selectionMode="multiple" sortDescriptor={sortDescriptor} onSortChange={setSortDescriptor} className="w-full min-w-fit">
        <TableHeader>
          <Column allowsSorting id="name" isRowHeader>
            Name
          </Column>
          <Column allowsSorting id="date">
            Added
          </Column>
          <Column allowsSorting id="lastSeen">
            Last Seen
          </Column>
          <Column allowsSorting id="type">
            Role
          </Column>
          <Column>
            Actions
          </Column>
        </TableHeader>
        <TableBody>
          {paginatedRows.map(user => (
            <Row key={user.email}>
              <Cell>
                <div className="flex items-center">
                  {user.profilePicture
                    ? (
                      <img src={user.profilePicture} alt="User avatar" className="mr-2" />
                      )
                    : (
                      <div className="w-8 h-8 rounded-full bg-gray-200 mr-2 flex items-center justify-center text-sm font-semibold uppercase">AB</div>
                      )}
                  <span>{user.name}</span>
                </div>
              </Cell>
              <Cell>{user.added.toLocaleDateString()}</Cell>
              <Cell>{user.lastSeen.toLocaleDateString()}</Cell>
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
      <Pagination
        total={rows.length}
        itemsPerPage={itemsPerPage}
        currentPage={currentPage}
        onPageChange={setCurrentPage}
      />
    </div>
  );
}
