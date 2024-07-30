import { createFileRoute } from "@tanstack/react-router";
import { Trans } from "@lingui/macro";
import { useApi } from "@/shared/lib/api/client";
import { TopMenu } from "@/shared/components/topMenu";
import { SharedSideMenu } from "@/shared/components/SharedSideMenu";

export const Route = createFileRoute("/admin/")({
  component: Home
});

export default function Home() {
  const { data } = useApi("/api/account-management/users", { params: { query: { PageSize: 1 } } });

  return (
    <div className="flex gap-4 w-full h-full border">
      <SharedSideMenu />
      <div className="flex flex-grow flex-col gap-4 pl-1 pr-6 py-3 overflow-x-auto">
        <div className="z-10">
          <TopMenu />
        </div>
        <div className="flex h-24 items-center justify-between space-x-2 mt-4 mb-4">
          <div className="text-foreground text-3xl font-semibold flex gap-2 flex-col mt-6">
            <h1 className="muted-foreground ">
              <Trans>Welcome home</Trans>
            </h1>
            <p className="text-muted-foreground text-sm font-normal whitespace-nowrap overflow-hidden text-ellipsis">
              <Trans>Here’s your overview of what’s going on.</Trans>
            </p>
          </div>
        </div>
        <div className="flex flex-row">
          <div className="text-muted-foreground p-6 bg-white rounded-xl shadow-md w-1/3">
            <div className="text-sm text-gray-800">
              <Trans>Total Users</Trans>
            </div>
            <div className="text-sm text-gray-500">
              <Trans>Add more in the Users menu</Trans>
            </div>
            <div className="py-2 text-black text-2xl font-semibold">
              {data?.totalCount ? <p>{data?.totalCount}</p> : <p>-</p>}
            </div>
          </div>
          <div className="p-6 bg-white rounded-xl shadow-md w-1/3 mx-6">
            <div className="text-sm text-gray-800">
              <Trans>Active Users</Trans>
            </div>
            <div className="text-sm text-gray-500">
              <Trans>Active users the past 30 days</Trans>
            </div>
            <div className="py-2 text-black text-2xl font-semibold">
              {data?.totalCount ? <p>{data?.totalCount}</p> : <p>-</p>}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
