import { ListFilterIcon, Search } from "lucide-react";
import React from "react";
import { Button } from "@/ui/components/Button";

export function UserQuerying() {
  const [searchTerm, setSearchTerm] = React.useState<string>("");
  return (
    <div className="flex justify-between mt-4 mb-4 space-x-2">
      <div className="relative">
        <input
          type="text"
          placeholder="Search"
          value={searchTerm}
          onChange={e => setSearchTerm(e.target.value)}
          className="border px-4 py-2 rounded pl-10 border-gray-300"
        />
        <Search className="absolute left-2 top-1/2 transform -translate-y-1/2 text-gray-400" />
      </div>

      <Button variant="secondary" className="flex gap-2 text-slate-700 text-sm font-semibold items-center">
        <ListFilterIcon size={16} />
        Filters
      </Button>
    </div>
  );
}
