import { Button, Input, Label } from "react-aria-components";
import { apiHooks } from "./lib/api/apiHook";
import { useState } from "react";

function App() {
  const [tenantName, setTenantName] = useState("");
  const createTenant = apiHooks.usePostApitenants();

  const handleCreateTenant = () => {
    console.log("createTenant", tenantName);
    createTenant.mutate({
      subdomain: tenantName,
      name: tenantName,
      email: `${tenantName}@foo.bar`,
    });
  };

  return (
    <div className="w-screen h-screen bg-slate-900 flex flex-col p-2 justify-center items-center">
      <div className="flex flex-col w-fit bg-slate-300 rounded-sm p-4 gap-2">
        <h1 className="text-xl font-bold">Create a tenant</h1>
        <Label htmlFor="input-tenant-name">Name</Label>
        <Input
          id="input-tenant-name"
          className="p-2 rounded-md border border-black"
          onChange={(e) => setTenantName(e.target.value)}
          value={tenantName}
          autoFocus
          placeholder="my-tenant"
        />
        <Button
          className="bg-slate-500 p-2 rounded-md text-white text-sm border border-black hover:bg-slate-400 w-fit"
          onPress={handleCreateTenant}
        >
          Create tenant!
        </Button>
      </div>
    </div>
  );
}

export default App;
