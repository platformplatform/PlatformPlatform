import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { InputGroup, InputGroupAddon, InputGroupButton, InputGroupInput } from "@repo/ui/components/InputGroup";
import { ToggleGroup, ToggleGroupItem } from "@repo/ui/components/ToggleGroup";
import { useDebounce } from "@repo/ui/hooks/useDebounce";
import { useNavigate } from "@tanstack/react-router";
import { SearchIcon, XIcon } from "lucide-react";
import { useEffect, useState } from "react";

import { SubscriptionPlan } from "@/shared/lib/api/client";
import { getSubscriptionPlanLabel } from "@/shared/lib/api/labels";

interface AccountsToolbarProps {
  search: string | undefined;
  plan: SubscriptionPlan | undefined;
}

export function AccountsToolbar({ search, plan }: Readonly<AccountsToolbarProps>) {
  const navigate = useNavigate();
  const [searchInput, setSearchInput] = useState(search ?? "");
  const debouncedSearch = useDebounce(searchInput, 500);

  useEffect(() => {
    if ((debouncedSearch || undefined) === search) {
      return;
    }
    navigate({
      to: "/accounts",
      search: (previous) => ({ ...previous, search: debouncedSearch || undefined, pageOffset: undefined })
    });
  }, [debouncedSearch, navigate, search]);

  useEffect(() => {
    setSearchInput(search ?? "");
  }, [search]);

  const handlePlanChange = (values: string[]) => {
    const next = values.find((value) => value !== "all") as SubscriptionPlan | undefined;
    navigate({
      to: "/accounts",
      search: (previous) => ({
        ...previous,
        plan: next,
        pageOffset: undefined
      })
    });
  };

  return (
    <div className="mb-4 flex flex-wrap items-center gap-3">
      <div className="max-w-[20rem] min-w-[14rem] flex-1">
        <InputGroup>
          <InputGroupAddon>
            <SearchIcon />
          </InputGroupAddon>
          <InputGroupInput
            type="text"
            role="searchbox"
            aria-label={t`Search`}
            placeholder={t`Search by name`}
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
        size="sm"
        spacing={2}
        aria-label={t`Plan`}
        value={plan ? [plan] : ["all"]}
        onValueChange={handlePlanChange}
      >
        <ToggleGroupItem value="all" className="min-w-[5rem] justify-center">
          <Trans>All</Trans>
        </ToggleGroupItem>
        {Object.values(SubscriptionPlan).map((value) => (
          <ToggleGroupItem key={value} value={value} className="min-w-[5rem] justify-center">
            {getSubscriptionPlanLabel(value)}
          </ToggleGroupItem>
        ))}
      </ToggleGroup>
    </div>
  );
}
