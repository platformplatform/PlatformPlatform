import { ListFilterIcon } from "lucide-react";
import { useCallback, useEffect, useState } from "react";
import { Button } from "@repo/ui/components/Button";
import { SearchField } from "@repo/ui/components/SearchField";
import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { useLocation, useNavigate } from "@tanstack/react-router";
import { type SortableUserProperties, type SortOrder, UserRole } from "@/shared/lib/api/client";
import { Select, SelectItem } from "@repo/ui/components/Select";
import { getUserRoleLabel } from "@/shared/lib/api/userRole";

// SearchParams interface defines the structure of URL query parameters
interface SearchParams {
  search: string | undefined;
  userRole: UserRole | undefined;
  orderBy: SortableUserProperties | undefined;
  sortOrder: SortOrder | undefined;
  pageOffset: number | undefined;
}

/**
 * UserQuerying component handles the user list filtering.
 * Uses URL parameters as the source of truth for all filters,
 * with a local state only for debounced search input.
 */
export function UserQuerying() {
  const navigate = useNavigate();
  const searchParams = (useLocation().search as SearchParams) ?? {};
  const [search, setSearch] = useState<string | undefined>(searchParams.search);

  // Updates URL parameters while preserving existing ones
  const updateFilter = useCallback(
    (params: Partial<SearchParams>) => {
      navigate({
        to: "/admin/users",
        search: (prev) => ({
          ...prev,
          ...params,
          pageOffset: prev.pageOffset === 0 ? undefined : prev.pageOffset
        })
      });
    },
    [navigate]
  );

  // Debounce search updates to avoid too many URL changes while typing
  useEffect(() => {
    const timeoutId = setTimeout(() => {
      updateFilter({ search: (search as string) || undefined });
    }, 500);

    return () => clearTimeout(timeoutId);
  }, [search, updateFilter]);

  return (
    <div className="flex items-center mt-4 mb-4 gap-2">
      <SearchField
        placeholder={t`Search`}
        value={search}
        onChange={setSearch}
        label={t`Search`}
        autoFocus
        className="min-w-[240px]"
      />

      <Select
        selectedKey={searchParams.userRole}
        onSelectionChange={(userRole) => {
          updateFilter({ userRole: (userRole as UserRole) || undefined });
        }}
        label={t`User Role`}
        placeholder={t`Any role`}
        className="w-[150px]"
      >
        <SelectItem id="">
          <Trans>Any role</Trans>
        </SelectItem>
        {Object.values(UserRole).map((userRole) => (
          <SelectItem id={userRole} key={userRole}>
            {getUserRoleLabel(userRole)}
          </SelectItem>
        ))}
      </Select>

      <Button variant="secondary" className="mt-6">
        <ListFilterIcon size={16} />
        <Trans>Filters</Trans>
      </Button>
    </div>
  );
}
