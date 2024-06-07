import { ListFilterIcon, Search } from "lucide-react";
import React from "react";
import { Button } from "@/ui/components/Button";

export function UserQuerying() {
  const [searchTerm, setSearchTerm] = React.useState<string>("");
  return (
    <div className="flex justify-between">
      <div className="relative">
        <input
          type="text"
          placeholder="Search users"
          value={searchTerm}
          onChange={e => setSearchTerm(e.target.value)}
          className="border px-4 py-2 rounded pl-10"
        />
        <Search className="absolute left-2 top-1/2 transform -translate-y-1/2 text-black" />
      </div>

      <Button variant="secondary" className="flex gap-2 text-slate-700 text-sm font-semibold items-center">
        <ListFilterIcon size={16} />
        Filters
      </Button>
    </div>
  );
}
