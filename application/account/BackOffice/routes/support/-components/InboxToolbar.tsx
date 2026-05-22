import { plural, t } from "@lingui/core/macro";
import { useLingui } from "@lingui/react";
import { Trans } from "@lingui/react/macro";
import { Button } from "@repo/ui/components/Button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger
} from "@repo/ui/components/DropdownMenu";
import { InputGroup, InputGroupAddon, InputGroupButton, InputGroupInput } from "@repo/ui/components/InputGroup";
import { ToggleGroup, ToggleGroupItem } from "@repo/ui/components/ToggleGroup";
import { useDebounce } from "@repo/ui/hooks/useDebounce";
import { useNavigate } from "@tanstack/react-router";
import { CheckIcon, ChevronDownIcon, SearchIcon, UserRoundIcon, XIcon } from "lucide-react";
import { useEffect, useState } from "react";

import type { SortableTicketProperties } from "@/shared/lib/api/client";

import { SupportTicketAssigneeFilter, SupportTicketCategory } from "@/shared/lib/api/client";

import { categoryIcons } from "./CategoryPill";
import { categoryLabels } from "./statusMaps";

const FILTERABLE_CATEGORIES: SupportTicketCategory[] = [
  SupportTicketCategory.Billing,
  SupportTicketCategory.Account,
  SupportTicketCategory.HowTo,
  SupportTicketCategory.Bug,
  SupportTicketCategory.Feature,
  SupportTicketCategory.Feedback
];

interface InboxToolbarProps {
  search: string | undefined;
  category: SupportTicketCategory | undefined;
  assignee: SupportTicketAssigneeFilter | undefined;
  resultCount: number | undefined;
}

export function InboxToolbar({ search, category, assignee, resultCount }: Readonly<InboxToolbarProps>) {
  const navigate = useNavigate();
  const { i18n } = useLingui();
  const [searchInput, setSearchInput] = useState(search ?? "");
  const debouncedSearch = useDebounce(searchInput, 500);

  useEffect(() => {
    if ((debouncedSearch || undefined) === search) return;
    navigate({
      to: "/support/tickets",
      search: (previous) => ({
        ...previous,
        orderBy: previous.orderBy as SortableTicketProperties | undefined,
        search: debouncedSearch || undefined,
        pageOffset: undefined,
        // Clear the side-pane selection: a new search can drop the previewed ticket's row.
        selectedTicketId: undefined
      })
    });
  }, [debouncedSearch, navigate, search]);

  useEffect(() => {
    setSearchInput(search ?? "");
  }, [search]);

  const handleCategoryChange = (value: string) => {
    const next = (value || undefined) as SupportTicketCategory | undefined;
    navigate({
      to: "/support/tickets",
      search: (previous) => ({
        ...previous,
        orderBy: previous.orderBy as SortableTicketProperties | undefined,
        category: next,
        pageOffset: undefined,
        selectedTicketId: undefined
      })
    });
  };

  const handleAssigneeChange = (value: SupportTicketAssigneeFilter | undefined) => {
    navigate({
      to: "/support/tickets",
      search: (previous) => ({
        ...previous,
        orderBy: previous.orderBy as SortableTicketProperties | undefined,
        assignee: value,
        pageOffset: undefined,
        selectedTicketId: undefined
      })
    });
  };

  return (
    <div className="mb-4 flex flex-wrap items-center gap-3">
      <div className="max-w-[22rem] min-w-[14rem] flex-1">
        <InputGroup>
          <InputGroupAddon>
            <SearchIcon />
          </InputGroupAddon>
          <InputGroupInput
            type="text"
            role="searchbox"
            aria-label={t`Search`}
            placeholder={t`Search subject, message, reporter, account, or ID`}
            value={searchInput}
            onChange={(event) => setSearchInput(event.target.value)}
            onKeyDown={(event) => event.key === "Escape" && searchInput && setSearchInput("")}
          />
          {searchInput && (
            <InputGroupAddon align="inline-end">
              <InputGroupButton onClick={() => setSearchInput("")} size="icon-xs" aria-label={t`Clear search`}>
                <XIcon />
              </InputGroupButton>
            </InputGroupAddon>
          )}
        </InputGroup>
      </div>

      <ToggleGroup
        variant="outline"
        aria-label={t`Category`}
        value={category ? [category] : []}
        onValueChange={(values) => handleCategoryChange(values.length === 0 ? "" : values[values.length - 1])}
      >
        {FILTERABLE_CATEGORIES.map((value) => {
          const Icon = categoryIcons[value];
          return (
            <ToggleGroupItem key={value} value={value} className="gap-1.5">
              <Icon className="size-3.5" aria-hidden={true} />
              {i18n._(categoryLabels[value])}
            </ToggleGroupItem>
          );
        })}
      </ToggleGroup>

      <AssigneeFilterMenu value={assignee} onChange={handleAssigneeChange} />

      <div className="flex-1" />

      {resultCount !== undefined && (
        <span className="text-sm text-muted-foreground tabular-nums">
          {plural(resultCount, { one: "# ticket", other: "# tickets" })}
        </span>
      )}

      {(search || category || assignee) && (
        <Button variant="ghost" size="sm" onClick={() => navigate({ to: "/support/tickets", search: () => ({}) })}>
          <Trans>Clear filters</Trans>
        </Button>
      )}
    </div>
  );
}

function AssigneeFilterMenu({
  value,
  onChange
}: {
  value: SupportTicketAssigneeFilter | undefined;
  onChange: (value: SupportTicketAssigneeFilter | undefined) => void;
}) {
  const label = (() => {
    switch (value) {
      case SupportTicketAssigneeFilter.Me:
        return t`Assigned to me`;
      case SupportTicketAssigneeFilter.Unassigned:
        return t`Unassigned`;
      default:
        return t`Any agent`;
    }
  })();

  return (
    <DropdownMenu trackingTitle="Assignee filter">
      <DropdownMenuTrigger
        render={
          <Button variant="outline" size="sm">
            <UserRoundIcon className="size-3.5" />
            {label}
            <ChevronDownIcon className="size-3.5" />
          </Button>
        }
      />
      <DropdownMenuContent align="start">
        <DropdownMenuItem onClick={() => onChange(undefined)} trackingLabel="Any agent">
          <Trans>Any agent</Trans>
          {!value && <CheckIcon className="ml-auto size-3.5" />}
        </DropdownMenuItem>
        <DropdownMenuItem onClick={() => onChange(SupportTicketAssigneeFilter.Me)} trackingLabel="Assigned to me">
          <Trans>Assigned to me</Trans>
          {value === SupportTicketAssigneeFilter.Me && <CheckIcon className="ml-auto size-3.5" />}
        </DropdownMenuItem>
        <DropdownMenuSeparator />
        <DropdownMenuItem onClick={() => onChange(SupportTicketAssigneeFilter.Unassigned)} trackingLabel="Unassigned">
          <Trans>Unassigned</Trans>
          {value === SupportTicketAssigneeFilter.Unassigned && <CheckIcon className="ml-auto size-3.5" />}
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
