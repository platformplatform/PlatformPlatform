import { ListFilterIcon } from "lucide-react";
import { useState } from "react";
import { Button } from "@repo/ui/components/Button";
import { SearchField } from "@repo/ui/components/SearchField";
import { Trans } from "@lingui/macro";

export function UserQuerying() {
  const [searchTerm, setSearchTerm] = useState<string>("");
  return (
    <div className="flex justify-between mt-4 mb-4 gap-2">
      <SearchField placeholder="Search" value={searchTerm} onChange={setSearchTerm} aria-label="Users" />

      <Button variant="secondary">
        <ListFilterIcon size={16} />
        <Trans>Filters</Trans>
      </Button>
    </div>
  );
}
