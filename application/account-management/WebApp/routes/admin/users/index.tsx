import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { useUserInfo } from "@repo/infrastructure/auth/hooks";
import { AppLayout } from "@repo/ui/components/AppLayout";
import { BreadcrumbItem, BreadcrumbLink, BreadcrumbPage } from "@repo/ui/components/Breadcrumb";
import { Link } from "@repo/ui/components/Link";
import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { useCallback, useEffect, useState } from "react";
import { z } from "zod";
import FederatedSideMenu from "@/federated-modules/sideMenu/FederatedSideMenu";
import { TopMenu } from "@/shared/components/topMenu";
import { api, type components, SortableUserProperties, SortOrder, UserRole, UserStatus } from "@/shared/lib/api/client";
import { ChangeUserRoleDialog } from "./-components/ChangeUserRoleDialog";
import { DeleteUserDialog } from "./-components/DeleteUserDialog";
import { UserProfileSidePane } from "./-components/UserProfileSidePane";
import { UserTable } from "./-components/UserTable";
import { UserTabNavigation } from "./-components/UserTabNavigation";
import { UserToolbar } from "./-components/UserToolbar";

type UserDetails = components["schemas"]["UserDetails"];

const userPageSearchSchema = z.object({
  search: z.string().optional(),
  userRole: z.nativeEnum(UserRole).nullable().optional(),
  userStatus: z.nativeEnum(UserStatus).nullable().optional(),
  startDate: z.string().optional(),
  endDate: z.string().optional(),
  orderBy: z.nativeEnum(SortableUserProperties).optional(),
  sortOrder: z.nativeEnum(SortOrder).optional(),
  pageOffset: z.number().optional(),
  userId: z.string().optional()
});

export const Route = createFileRoute("/admin/users/")({
  component: UsersPage,
  validateSearch: userPageSearchSchema
});

export default function UsersPage() {
  const userInfo = useUserInfo();
  const [selectedUsers, setSelectedUsers] = useState<UserDetails[]>([]);
  const [profileUser, setProfileUser] = useState<UserDetails | null>(null);
  const [userToDelete, setUserToDelete] = useState<UserDetails | null>(null);
  const [userToChangeRole, setUserToChangeRole] = useState<UserDetails | null>(null);
  const [isInitialLoad, setIsInitialLoad] = useState(true);
  const [tableUsers, setTableUsers] = useState<UserDetails[]>([]);

  const navigate = useNavigate({ from: Route.fullPath });
  const { userId } = Route.useSearch();

  const canSeeDeletedUsers = userInfo?.role === "Owner" || userInfo?.role === "Admin";

  const handleCloseProfile = useCallback(() => {
    setProfileUser(null);
    navigate({ search: (prev) => ({ ...prev, userId: undefined }) });

    if (selectedUsers.length === 1) {
      setTimeout(() => {
        const selectedRow = document.querySelector(`[data-key="${selectedUsers[0].id}"]`);
        if (selectedRow) {
          (selectedRow as HTMLElement).focus();
        }
      }, 0);
    }
  }, [navigate, selectedUsers]);

  const handleViewProfile = useCallback(
    (user: UserDetails | null) => {
      setProfileUser(user);
      if (user) {
        navigate({ search: (prev) => ({ ...prev, userId: user.id }) });
      } else {
        navigate({ search: (prev) => ({ ...prev, userId: undefined }) });
      }
    },
    [navigate]
  );

  const { data: userData, isLoading: isLoadingUser } = api.useQuery(
    "get",
    "/api/account-management/users/{id}",
    { params: { path: { id: userId || "" } } },
    { enabled: !!userId }
  );

  useEffect(() => {
    if (userId && userData) {
      setProfileUser(userData);
      if (isInitialLoad) {
        setSelectedUsers([userData]);
        setIsInitialLoad(false);
      }
    } else if (!userId && isInitialLoad) {
      setIsInitialLoad(false);
    }
  }, [userId, userData, isInitialLoad]);

  const handleDeleteUser = useCallback((user: UserDetails) => {
    setUserToDelete(user);
  }, []);

  const handleChangeRole = useCallback((user: UserDetails) => {
    setUserToChangeRole(user);
  }, []);

  const handleUsersLoaded = useCallback((users: UserDetails[]) => {
    setTableUsers(users);
  }, []);

  const isUserInCurrentView = profileUser ? tableUsers.some((u) => u.id === profileUser.id) : true;

  const tableUser = profileUser ? tableUsers.find((u) => u.id === profileUser.id) : null;
  const isDataNewer = !!(
    userData &&
    tableUser &&
    userData.modifiedAt &&
    tableUser.modifiedAt &&
    new Date(userData.modifiedAt).getTime() !== new Date(tableUser.modifiedAt).getTime()
  );

  const getSidePane = () => {
    if (profileUser) {
      return (
        <UserProfileSidePane
          user={profileUser}
          isOpen={!!profileUser}
          onClose={handleCloseProfile}
          onDeleteUser={handleDeleteUser}
          isUserInCurrentView={isUserInCurrentView}
          isDataNewer={isDataNewer}
          isLoading={isLoadingUser || !!(userId && profileUser.id !== userId)}
        />
      );
    }
    return undefined;
  };

  return (
    <>
      <FederatedSideMenu currentSystem="account-management" />
      <AppLayout
        sidePane={getSidePane()}
        topMenu={
          <TopMenu>
            <BreadcrumbItem>
              <BreadcrumbLink render={<Link href="/admin/users" variant="secondary" underline={false} />}>
                <Trans>Users</Trans>
              </BreadcrumbLink>
            </BreadcrumbItem>
            <BreadcrumbPage>
              <Trans>All users</Trans>
            </BreadcrumbPage>
          </TopMenu>
        }
        title={t`Users`}
        subtitle={t`Manage your users and permissions here.`}
      >
        <div className="flex min-h-0 flex-1 flex-col">
          {canSeeDeletedUsers && <UserTabNavigation activeTab="all-users" />}
          <div className="sticky top-7 z-10 -mx-4 bg-background px-4 pt-3 sm:static sm:z-auto sm:mx-0 sm:px-0 sm:pt-0">
            <UserToolbar selectedUsers={selectedUsers} onSelectedUsersChange={setSelectedUsers} />
          </div>
          <div className="flex min-h-0 flex-1 flex-col">
            <UserTable
              selectedUsers={selectedUsers}
              onSelectedUsersChange={setSelectedUsers}
              onViewProfile={handleViewProfile}
              onDeleteUser={handleDeleteUser}
              onChangeRole={handleChangeRole}
              onUsersLoaded={handleUsersLoaded}
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
          navigate({ search: (prev) => ({ ...prev, userId: undefined }) });
        }}
      />
    </>
  );
}
