import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { AppLayout } from "@repo/ui/components/AppLayout";
import { Button } from "@repo/ui/components/Button";
import { Empty, EmptyContent, EmptyDescription, EmptyHeader, EmptyMedia, EmptyTitle } from "@repo/ui/components/Empty";
import { SidebarInset, SidebarProvider } from "@repo/ui/components/Sidebar";
import { keepPreviousData } from "@tanstack/react-query";
import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { UsersIcon } from "lucide-react";
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

function UsersSearchPage() {
  const { search, roles, activity, pageOffset } = Route.useSearch();
  const trimmed = search?.trim() ?? "";
  const hasSearchOrFilter = trimmed.length > 0 || (roles?.length ?? 0) > 0 || activity !== undefined;

  const { data, isLoading } = api.useQuery(
    "get",
    "/api/back-office/users",
    {
      params: {
        query: {
          Search: trimmed.length > 0 ? trimmed : undefined,
          Roles: roles,
          Activity: activity,
          PageOffset: pageOffset
        }
      }
    },
    { placeholderData: keepPreviousData }
  );

  const users = data?.users ?? [];
  const showNoResults = !isLoading && users.length === 0 && hasSearchOrFilter;
  const showEmpty = !isLoading && users.length === 0 && !hasSearchOrFilter;

  return (
    <SidebarProvider>
      <BackOfficeSideMenu />
      <SidebarInset>
        <AppLayout
          variant="center"
          maxWidth="64rem"
          browserTitle={t`Users`}
          title={t`Users`}
          subtitle={t`All users across every account, most recently seen first. Search and filter to narrow down.`}
        >
          <UsersToolbar search={search} roles={roles ?? []} activity={activity} />

          {showNoResults ? (
            <UsersNoResultsEmpty />
          ) : showEmpty ? (
            <Empty>
              <EmptyHeader>
                <EmptyMedia variant="icon">
                  <UsersIcon />
                </EmptyMedia>
                <EmptyTitle>
                  <Trans>No users yet</Trans>
                </EmptyTitle>
                <EmptyDescription>
                  <Trans>Users will appear here as accounts are created.</Trans>
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

function UsersNoResultsEmpty() {
  const navigate = useNavigate();
  return (
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
      <EmptyContent>
        <Button variant="outline" size="sm" onClick={() => navigate({ to: "/users", search: () => ({}) })}>
          <Trans>Clear filters</Trans>
        </Button>
      </EmptyContent>
    </Empty>
  );
}
