import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { AppLayout } from "@repo/ui/components/AppLayout";
import { Empty, EmptyDescription, EmptyHeader, EmptyMedia, EmptyTitle } from "@repo/ui/components/Empty";
import { SidebarInset, SidebarProvider } from "@repo/ui/components/Sidebar";
import { keepPreviousData } from "@tanstack/react-query";
import { createFileRoute } from "@tanstack/react-router";
import { SearchIcon, UsersIcon } from "lucide-react";
import { z } from "zod";

import { BackOfficeSideMenu } from "@/shared/components/BackOfficeSideMenu";
import { api, UserActivityFilter, UserRole } from "@/shared/lib/api/client";

import { UsersTable } from "./-components/UsersTable";
import { UsersToolbar } from "./-components/UsersToolbar";

const usersSearchSchema = z.object({
  search: z.string().optional(),
  roles: z.array(z.nativeEnum(UserRole)).max(10).optional(),
  activity: z.nativeEnum(UserActivityFilter).optional(),
  pageOffset: z.number().int().nonnegative().optional()
});

export const Route = createFileRoute("/users/")({
  staticData: { trackingTitle: "Users" },
  validateSearch: usersSearchSchema,
  component: UsersSearchPage
});

const minSearchLength = 2;

function UsersSearchPage() {
  const { search, roles, activity, pageOffset } = Route.useSearch();
  const trimmed = search?.trim() ?? "";
  const hasSearch = trimmed.length >= minSearchLength;

  const { data, isLoading } = api.useQuery(
    "get",
    "/api/back-office/users",
    {
      params: {
        query: {
          Search: trimmed,
          Roles: roles,
          Activity: activity,
          PageOffset: pageOffset
        }
      }
    },
    { placeholderData: keepPreviousData, enabled: hasSearch }
  );

  const users = data?.users ?? [];
  const showEmptySearch = !hasSearch;
  const showNoResults = hasSearch && !isLoading && users.length === 0;

  return (
    <SidebarProvider>
      <BackOfficeSideMenu />
      <SidebarInset>
        <AppLayout
          variant="center"
          maxWidth="64rem"
          browserTitle={t`Users`}
          title={t`Users`}
          subtitle={t`Search users by email, name, or tenant.`}
        >
          <UsersToolbar search={search} roles={roles ?? []} activity={activity} />

          {showEmptySearch ? (
            <Empty>
              <EmptyHeader>
                <EmptyMedia variant="icon">
                  <SearchIcon />
                </EmptyMedia>
                <EmptyTitle>
                  <Trans>Type to search</Trans>
                </EmptyTitle>
                <EmptyDescription>
                  <Trans>Search by user email, first or last name, or tenant name. At least 2 characters.</Trans>
                </EmptyDescription>
              </EmptyHeader>
            </Empty>
          ) : showNoResults ? (
            <Empty>
              <EmptyHeader>
                <EmptyMedia variant="icon">
                  <UsersIcon />
                </EmptyMedia>
                <EmptyTitle>
                  <Trans>No users match your search</Trans>
                </EmptyTitle>
                <EmptyDescription>
                  <Trans>Try a different search term or clear the role and activity filters.</Trans>
                </EmptyDescription>
              </EmptyHeader>
            </Empty>
          ) : (
            <div className="flex min-h-0 flex-1 flex-col">
              <UsersTable
                users={users}
                isLoading={isLoading}
                totalPages={data?.totalPages ?? 0}
                currentPageOffset={data?.currentPageOffset ?? 0}
              />
            </div>
          )}
        </AppLayout>
      </SidebarInset>
    </SidebarProvider>
  );
}
