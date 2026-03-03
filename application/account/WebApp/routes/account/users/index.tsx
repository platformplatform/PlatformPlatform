import { t } from "@lingui/core/macro";
import { useUserInfo } from "@repo/infrastructure/auth/hooks";
import { useUser, type useUsers } from "@repo/infrastructure/sync/hooks";
import { AppLayout } from "@repo/ui/components/AppLayout";
import { createFileRoute, useNavigate } from "@tanstack/react-router";
import { useCallback, useEffect, useState } from "react";
import { z } from "zod";

import { UserRole } from "@/shared/lib/api/client";
import { SortableUserProperties, SortOrder } from "@/shared/lib/api/sortTypes";
import { UserStatus } from "@/shared/lib/api/userStatus";

import { ChangeUserRoleDialog } from "./-components/ChangeUserRoleDialog";
import { DeleteUserDialog } from "./-components/DeleteUserDialog";
import { UserProfileSidePane } from "./-components/UserProfileSidePane";
import { UserTable } from "./-components/UserTable";
import { UserTabNavigation } from "./-components/UserTabNavigation";
import { UserToolbar } from "./-components/UserToolbar";

type ElectricUser = ReturnType<typeof useUsers>["data"][number];

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

export const Route = createFileRoute("/account/users/")({
  staticData: { trackingTitle: "Users" },
  component: UsersPage,
  validateSearch: userPageSearchSchema
});

export default function UsersPage() {
  const userInfo = useUserInfo();
  const [selectedUsers, setSelectedUsers] = useState<ElectricUser[]>([]);
  const [profileUser, setProfileUser] = useState<ElectricUser | null>(null);
  const [userToDelete, setUserToDelete] = useState<ElectricUser | null>(null);
  const [userToChangeRole, setUserToChangeRole] = useState<ElectricUser | null>(null);
  const [tableUsers, setTableUsers] = useState<ElectricUser[]>([]);

  const navigate = useNavigate({ from: Route.fullPath });
  const { userId } = Route.useSearch();

  const canSeeDeletedUsers = userInfo?.role === "Owner" || userInfo?.role === "Admin";

  const { data: linkedUser } = useUser(userId ?? "");

  useEffect(() => {
    if (userId && linkedUser) {
      setProfileUser(linkedUser);
    }
  }, [userId, linkedUser]);

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
    (user: ElectricUser | null) => {
      setProfileUser(user);
      if (user) {
        navigate({ search: (prev) => ({ ...prev, userId: user.id }) });
      } else {
        navigate({ search: (prev) => ({ ...prev, userId: undefined }) });
      }
    },
    [navigate]
  );

  const handleDeleteUser = useCallback((user: ElectricUser) => {
    setUserToDelete(user);
  }, []);

  const handleChangeRole = useCallback((user: ElectricUser) => {
    setUserToChangeRole(user);
  }, []);

  const handleUsersLoaded = useCallback((users: ElectricUser[]) => {
    setTableUsers(users);
  }, []);

  const isUserInCurrentView = profileUser ? tableUsers.some((u) => u.id === profileUser.id) : true;

  const getSidePane = () => {
    if (profileUser) {
      return (
        <UserProfileSidePane
          user={profileUser}
          isOpen={!!profileUser}
          onClose={handleCloseProfile}
          onDeleteUser={(user) => setUserToDelete(user as ElectricUser)}
          isUserInCurrentView={isUserInCurrentView}
        />
      );
    }
    return undefined;
  };

  return (
    <>
      <AppLayout
        variant="center"
        sidePane={getSidePane()}
        maxWidth="64rem"
        title={t`Users`}
        subtitle={t`Manage your users and permissions here.`}
      >
        {canSeeDeletedUsers && <UserTabNavigation activeTab="all-users" />}
        <div className="flex min-h-0 flex-1 flex-col">
          <div className="max-sm:sticky max-sm:top-12">
            <UserToolbar
              selectedUsers={selectedUsers}
              onSelectedUsersChange={(users) => setSelectedUsers(users as ElectricUser[])}
            />
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
