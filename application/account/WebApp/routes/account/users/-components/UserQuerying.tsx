import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { trackInteraction } from "@repo/infrastructure/applicationInsights/ApplicationInsightsProvider";
import { Button } from "@repo/ui/components/Button";
import { DateRangePicker } from "@repo/ui/components/DateRangePicker";
import { Field, FieldLabel } from "@repo/ui/components/Field";
import { InputGroup, InputGroupAddon, InputGroupButton, InputGroupInput } from "@repo/ui/components/InputGroup";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@repo/ui/components/Select";
import { Tooltip, TooltipContent, TooltipTrigger } from "@repo/ui/components/Tooltip";
import { format } from "date-fns";
import { Filter, FilterX, SearchIcon, XIcon } from "lucide-react";

import { UserRole } from "@/shared/lib/api/client";
import { getUserRoleLabel } from "@/shared/lib/api/userRole";
import { getUserStatusLabel, UserStatus } from "@/shared/lib/api/userStatus";

import { UserFilterDialog } from "./UserFilterDialog";
import { useUserFilters } from "./useUserFilters";

interface UserQueryingProps {
  onFiltersUpdated?: () => void;
  onFiltersExpandedChange?: (expanded: boolean) => void;
}

export function UserQuerying({ onFiltersUpdated, onFiltersExpandedChange }: UserQueryingProps = {}) {
  const filters = useUserFilters({ onFiltersUpdated, onFiltersExpandedChange });

  return (
    <div ref={filters.containerRef} className="flex items-center gap-2">
      <Field className={filters.showAllFilters ? "w-60 shrink-0" : "max-w-60 min-w-32 flex-1"}>
        <FieldLabel>{t`Search`}</FieldLabel>
        <InputGroup>
          <InputGroupAddon>
            <SearchIcon />
          </InputGroupAddon>
          <InputGroupInput
            type="text"
            role="searchbox"
            placeholder={t`Search`}
            value={filters.search}
            onChange={(e) => filters.setSearch(e.target.value)}
            onKeyDown={(e) => e.key === "Escape" && filters.search && filters.setSearch("")}
          />
          {filters.search && (
            <InputGroupAddon align="inline-end">
              <InputGroupButton onClick={() => filters.setSearch("")} size="icon-xs" aria-label={t`Clear search`}>
                <XIcon />
              </InputGroupButton>
            </InputGroupAddon>
          )}
        </InputGroup>
      </Field>

      {filters.showAllFilters && (
        <InlineFilters
          dateRange={filters.dateRange}
          userRole={filters.searchParams.userRole}
          userStatus={filters.searchParams.userStatus}
          updateFilter={filters.updateFilter}
        />
      )}

      <Tooltip>
        <TooltipTrigger
          render={
            <Button
              variant="secondary"
              size="icon"
              className={filters.showAllFilters ? "relative mt-auto" : "relative mt-8"}
              aria-label={filters.showAllFilters ? t`Clear filters` : t`Show search filters`}
              data-testid="filter-button"
              onClick={filters.handleFilterButtonClick}
            >
              {filters.showAllFilters ? (
                <FilterX size={16} aria-label={t`Clear filters`} />
              ) : (
                <Filter size={16} aria-label={t`Show search filters`} />
              )}
              {filters.activeFilterCount > 0 && !filters.showAllFilters && (
                <span className="absolute -top-1 -right-1 flex size-5 items-center justify-center rounded-full bg-primary text-xs font-medium text-primary-foreground">
                  {filters.activeFilterCount}
                </span>
              )}
            </Button>
          }
        />
        <TooltipContent>
          {filters.showAllFilters ? <Trans>Clear filters</Trans> : <Trans>Show search filters</Trans>}
        </TooltipContent>
      </Tooltip>

      <UserFilterDialog
        isOpen={filters.isFilterPanelOpen}
        onOpenChange={filters.setIsFilterPanelOpen}
        search={filters.search}
        onSearchChange={filters.setSearch}
        dateRange={filters.dateRange}
        userRole={filters.searchParams.userRole}
        userStatus={filters.searchParams.userStatus}
        hasSearch={Boolean(filters.searchParams.search)}
        activeFilterCount={filters.activeFilterCount}
        updateFilter={filters.updateFilter}
        onClearAll={filters.clearAllFilters}
      />
    </div>
  );
}

function InlineFilters({
  dateRange,
  userRole,
  userStatus,
  updateFilter
}: Readonly<{
  dateRange: ReturnType<typeof useUserFilters>["dateRange"];
  userRole: UserRole | undefined;
  userStatus: UserStatus | undefined;
  updateFilter: ReturnType<typeof useUserFilters>["updateFilter"];
}>) {
  return (
    <>
      <DateRangePicker
        value={dateRange}
        onChange={(range) => {
          trackInteraction("User filters", "interaction", "Date filter");
          updateFilter({
            startDate: range ? format(range.start, "yyyy-MM-dd") : undefined,
            endDate: range ? format(range.end, "yyyy-MM-dd") : undefined
          });
        }}
        label={t`Modified date`}
        placeholder={t`Select dates`}
      />

      <Field className="flex flex-col">
        <FieldLabel>{t`User role`}</FieldLabel>
        <Select
          value={userRole ?? ""}
          onValueChange={(value) => {
            trackInteraction("User filters", "interaction", "Role filter");
            updateFilter({ userRole: (value as UserRole) || undefined });
          }}
        >
          <SelectTrigger aria-label={t`User role`} className="min-w-28">
            <SelectValue>{(value: string) => (value ? getUserRoleLabel(value as UserRole) : t`Any role`)}</SelectValue>
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="">
              <Trans>Any role</Trans>
            </SelectItem>
            {Object.values(UserRole).map((role) => (
              <SelectItem value={role} key={role}>
                {getUserRoleLabel(role)}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </Field>

      <Field className="flex flex-col">
        <FieldLabel>{t`User status`}</FieldLabel>
        <Select
          value={userStatus ?? ""}
          onValueChange={(value) => {
            trackInteraction("User filters", "interaction", "Status filter");
            updateFilter({ userStatus: (value as UserStatus) || undefined });
          }}
        >
          <SelectTrigger aria-label={t`User status`} className="min-w-28">
            <SelectValue>
              {(value: string) => (value ? getUserStatusLabel(value as UserStatus) : t`Any status`)}
            </SelectValue>
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="">
              <Trans>Any status</Trans>
            </SelectItem>
            {Object.values(UserStatus).map((status) => (
              <SelectItem value={status} key={status}>
                {getUserStatusLabel(status)}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </Field>
    </>
  );
}
