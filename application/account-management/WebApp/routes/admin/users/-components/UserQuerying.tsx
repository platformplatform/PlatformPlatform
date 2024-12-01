import { ListFilterIcon } from "lucide-react";
import { useCallback, useEffect, useState } from "react";
import { Button } from "@repo/ui/components/Button";
import { SearchField } from "@repo/ui/components/SearchField";
import { t, Trans } from "@lingui/macro";
import { useNavigate, useSearch } from "@tanstack/react-router";

export function UserQuerying() {
  const navigate = useNavigate();
  const { search: urlSearch = "" } = useSearch({ strict: false });
  const [search, setSearch] = useState<string>(urlSearch);

  const updateSearch = useCallback(
    (value: string) => {
      navigate({
        to: "/admin/users",
        search: (prev) => ({
          ...prev,
          search: value || undefined,
          pageOffset: 0
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

  const handleSearchChange = (value: string) => {
    setSearch(value);
  };

  useEffect(() => {
    setSearch(urlSearch);
  }, [urlSearch]);

  return (
    <div className="flex justify-between mt-4 mb-4 gap-2">
      <SearchField
        placeholder={t`Search`}
        value={search}
        onChange={handleSearchChange}
        aria-label={t`Search`}
        autoFocus
      />

      <Button variant="secondary">
        <ListFilterIcon size={16} />
        <Trans>Filters</Trans>
      </Button>
    </div>
  );
}
