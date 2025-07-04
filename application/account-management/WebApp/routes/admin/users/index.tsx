import { SharedSideMenu } from "@/shared/components/SharedSideMenu";
import { TopMenu } from "@/shared/components/topMenu";
import { SortOrder, SortableUserProperties, UserRole, UserStatus, api, type components } from "@/shared/lib/api/client";
import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { AppLayout } from "@repo/ui/components/AppLayout";
import { Breadcrumb } from "@repo/ui/components/Breadcrumbs";
import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { useEffect, useState } from "react";
import { z } from "zod";
import { ChangeUserRoleDialog } from "./-components/ChangeUserRoleDialog";
import { DeleteUserDialog } from "./-components/DeleteUserDialog";
import { UserProfileSidePane } from "./-components/UserProfileSidePane";
import { UserTable } from "./-components/UserTable";
import { UserToolbar } from "./-components/UserToolbar";

type UserDetails = components["schemas"]["UserDetails"];

const userPageSearchSchema = z.object({
  search: z.string().optional(),
  userRole: z.nativeEnum(UserRole).nullable().optional(),
  userStatus: z.nativeEnum(UserStatus).nullable().optional(),
  startDate: z.string().optional(),
  endDate: z.string().optional(),
  orderBy: z.nativeEnum(SortableUserProperties).default(SortableUserProperties.Name).optional(),
  sortOrder: z.nativeEnum(SortOrder).default(SortOrder.Ascending).optional(),
  pageOffset: z.number().default(0).optional(),
  userId: z.string().optional()
});

export const Route = createFileRoute("/admin/users/")({
  component: UsersPage,
  validateSearch: userPageSearchSchema
});

export default function UsersPage() {
  const [selectedUsers, setSelectedUsers] = useState<UserDetails[]>([]);
  const [profileUser, setProfileUser] = useState<UserDetails | null>(null);
  const [userToDelete, setUserToDelete] = useState<UserDetails | null>(null);
  const [userToChangeRole, setUserToChangeRole] = useState<UserDetails | null>(null);
  const [isInitialLoad, setIsInitialLoad] = useState(true);
  const navigate = useNavigate({ from: Route.fullPath });
  const { userId } = Route.useSearch();

  const handleCloseProfile = () => {
    setProfileUser(null);
    setSelectedUsers([]);
    navigate({ search: (prev) => ({ ...prev, userId: undefined }) });
  };

  const handleViewProfile = (user: UserDetails | null) => {
    setProfileUser(user);
    if (user) {
      navigate({ search: (prev) => ({ ...prev, userId: user.id }) });
    } else {
      navigate({ search: (prev) => ({ ...prev, userId: undefined }) });
    }
  };

  const { data: usersData } = api.useQuery("get", "/api/account-management/users", {
    params: {
      query: {
        PageSize: 1000
      }
    },
    enabled: !!userId && isInitialLoad
  });

  useEffect(() => {
    if (userId && usersData?.users && isInitialLoad) {
      const userToOpen = usersData.users.find((u) => u.id === userId);
      if (userToOpen) {
        setProfileUser(userToOpen);
        setSelectedUsers([userToOpen]);
      }
      setIsInitialLoad(false);
    } else if (!userId && isInitialLoad) {
      setIsInitialLoad(false);
    }
  }, [userId, usersData?.users, isInitialLoad]);

  const handleDeleteUser = (user: UserDetails) => {
    setUserToDelete(user);
  };

  const handleChangeRole = (user: UserDetails) => {
    setUserToChangeRole(user);
  };

  return (
    <>
      <SharedSideMenu ariaLabel={t`Toggle collapsed menu`} />
      <AppLayout
        sidePane={
          profileUser ? (
            <UserProfileSidePane
              user={profileUser}
              isOpen={profileUser !== null}
              onClose={handleCloseProfile}
              onDeleteUser={handleDeleteUser}
            />
          ) : undefined
        }
        topMenu={
          <TopMenu>
            <Breadcrumb href="/admin/users">
              <Trans>Users</Trans>
            </Breadcrumb>
            <Breadcrumb>
              <Trans>All users</Trans>
            </Breadcrumb>
          </TopMenu>
        }
      >
        <div className="flex h-full flex-col">
          <h1>
            <Trans>Users</Trans>
          </h1>
          <p>
            <Trans>Manage your users and permissions here.</Trans>
          </p>

          <div className="mb-4">
            <UserToolbar selectedUsers={selectedUsers} onSelectedUsersChange={setSelectedUsers} />
          </div>
          <div className="min-h-0 flex-1">
            <UserTable
              selectedUsers={selectedUsers}
              onSelectedUsersChange={setSelectedUsers}
              onViewProfile={handleViewProfile}
              onDeleteUser={handleDeleteUser}
              onChangeRole={handleChangeRole}
            />
          </div>
        </div>
      </AppLayout>

      <ChangeUserRoleDialog
        user={userToChangeRole}
        isOpen={userToChangeRole !== null}
        onOpenChange={(isOpen) => !isOpen && setUserToChangeRole(null)}
      />

      <DeleteUserDialog
        users={userToDelete ? [userToDelete] : []}
        isOpen={userToDelete !== null}
        onOpenChange={(isOpen) => !isOpen && setUserToDelete(null)}
        onUsersDeleted={() => {
          setSelectedUsers([]);
          setProfileUser(null);
        }}
      />
    </>
  );
}
