import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Cell, Column, Row, Table, TableHeader } from "@repo/ui/components/Table";
import { Text } from "@repo/ui/components/Text";
import { useViewportResize } from "@repo/ui/hooks/useViewportResize";
import { isMediumViewportOrLarger, isSmallViewportOrLarger, isTouchDevice } from "@repo/ui/utils/responsive";
import { useCallback, useState } from "react";
import type { Selection, SortDescriptor } from "react-aria-components";
import { TableBody } from "react-aria-components";
import type { TeamDetails } from "../-data/mockTeams";

interface TeamsTableProps {
  teams: TeamDetails[];
  selectedTeam: TeamDetails | null;
  onSelectedTeamChange: (team: TeamDetails | null) => void;
  isTeamDetailsPaneOpen?: boolean;
}

export function TeamsTable({
  teams,
  selectedTeam,
  onSelectedTeamChange,
  isTeamDetailsPaneOpen
}: Readonly<TeamsTableProps>) {
  const [sortDescriptor, setSortDescriptor] = useState<SortDescriptor>({
    column: "name",
    direction: "ascending"
  });
  const isMobile = useViewportResize();

  const sortedTeams = [...teams].sort((a, b) => {
    const column = sortDescriptor.column as keyof TeamDetails;
    const direction = sortDescriptor.direction === "ascending" ? 1 : -1;

    if (column === "name" || column === "description") {
      return direction * a[column].localeCompare(b[column]);
    }

    if (column === "memberCount") {
      return direction * (a.memberCount - b.memberCount);
    }

    return 0;
  });

  const handleSelectionChange = useCallback(
    (keys: Selection) => {
      if (keys === "all") {
        onSelectedTeamChange(null);
        return;
      }

      const selectedTeamsList = sortedTeams.filter((team) => keys.has(team.id));

      if (selectedTeamsList.length !== 1) {
        onSelectedTeamChange(null);
        return;
      }

      if (isTouchDevice() || !isMediumViewportOrLarger()) {
        onSelectedTeamChange(selectedTeamsList[0]);
      } else if (isMediumViewportOrLarger()) {
        onSelectedTeamChange(selectedTeamsList[0]);
      }
    },
    [sortedTeams, onSelectedTeamChange]
  );

  const handleSortChange = useCallback((newSortDescriptor: SortDescriptor) => {
    setSortDescriptor(newSortDescriptor);
  }, []);

  return (
    <div className="min-h-48 flex-1">
      <Table
        selectionMode={isTouchDevice() || !isMediumViewportOrLarger() ? "single" : "multiple"}
        selectionBehavior="replace"
        selectedKeys={selectedTeam ? new Set([selectedTeam.id]) : new Set()}
        onSelectionChange={handleSelectionChange}
        sortDescriptor={sortDescriptor}
        onSortChange={handleSortChange}
        aria-label={t`Teams`}
        className={isMobile ? "[&>div>div>div]:-webkit-overflow-scrolling-touch" : ""}
        disableHorizontalScroll={isTeamDetailsPaneOpen}
      >
        <TableHeader>
          <Column
            allowsSorting={true}
            id="name"
            isRowHeader={true}
            minWidth={isSmallViewportOrLarger() ? 200 : undefined}
          >
            <Trans>Name</Trans>
          </Column>
          {isSmallViewportOrLarger() && (
            <Column minWidth={250} allowsSorting={true} id="description">
              <Trans>Description</Trans>
            </Column>
          )}
          {isMediumViewportOrLarger() && (
            <Column minWidth={100} defaultWidth={120} allowsSorting={true} id="memberCount">
              <Trans>Members</Trans>
            </Column>
          )}
        </TableHeader>
        <TableBody>
          {sortedTeams.map((team) => (
            <Row key={team.id} id={team.id}>
              <Cell>
                <div className="flex h-14 w-full items-center gap-2 p-0">
                  <div className="flex min-w-0 flex-1 flex-col">
                    <Text className="truncate text-foreground">{team.name}</Text>
                    {!isSmallViewportOrLarger() && (
                      <Text className="truncate text-muted-foreground text-sm">{team.description}</Text>
                    )}
                  </div>
                </div>
              </Cell>
              {isSmallViewportOrLarger() && (
                <Cell>
                  <Text className="h-full w-full justify-start p-0 text-left font-normal">{team.description}</Text>
                </Cell>
              )}
              {isMediumViewportOrLarger() && (
                <Cell>
                  <Text className="h-full w-full justify-start p-0 text-left font-normal">
                    {team.memberCount} {team.memberCount === 1 ? t`member` : t`members`}
                  </Text>
                </Cell>
              )}
            </Row>
          ))}
        </TableBody>
      </Table>
    </div>
  );
}
