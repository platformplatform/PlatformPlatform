import { ListFilterIcon } from "lucide-react";
import { useCallback, useEffect, useState } from "react";
import { Button } from "@repo/ui/components/Button";
import { SearchField } from "@repo/ui/components/SearchField";
import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { useNavigate, useLocation } from "@tanstack/react-router";
import type { SortableUserProperties, SortOrder } from "@/shared/lib/api/client";

interface SearchParams {
  search?: string;
  orderBy?: SortableUserProperties;
  sortOrder?: SortOrder;
  pageOffset?: number;
}

export function UserQuerying() {
  const navigate = useNavigate();
  const location = useLocation();

  const searchParams = location.search as SearchParams;
  const { search: urlSearch = "" } = searchParams ?? {};

  const [search, setSearch] = useState<string>(urlSearch);

  const updateSearch = useCallback(
    (value: string) => {
      navigate({
        to: "/admin/users",
        search: (prev) => ({
          ...prev,
          search: value || undefined,
          pageOffset: prev.pageOffset === 0 ? undefined : prev.pageOffset
        })
      });
    },
    [navigate]
  );

  useEffect(() => {
    const timeoutId = setTimeout(() => {
      updateSearch(search);
    }, 700);

    return () => clearTimeout(timeoutId);
  }, [search, updateSearch]);

  useEffect(() => {
    setSearch(urlSearch);
  }, [urlSearch]);

  return (
    <div className="flex justify-between mt-4 mb-4 gap-2">
      <SearchField placeholder={t`Search`} value={search} onChange={setSearch} aria-label={t`Search`} autoFocus />

      <Button variant="secondary">
        <ListFilterIcon size={16} />
        <Trans>Filters</Trans>
      </Button>
    </div>
  );
}
