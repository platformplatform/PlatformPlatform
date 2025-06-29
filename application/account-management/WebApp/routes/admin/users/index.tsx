import { SharedSideMenu } from "@/shared/components/SharedSideMenu";
import { TopMenu } from "@/shared/components/topMenu";
import { SortOrder, SortableUserProperties, UserRole, UserStatus, type components } from "@/shared/lib/api/client";
import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { AppLayout } from "@repo/ui/components/AppLayout";
import { Breadcrumb } from "@repo/ui/components/Breadcrumbs";
import { createFileRoute } from "@tanstack/react-router";
import { useState } from "react";
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
  pageOffset: z.number().default(0).optional()
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

  const handleCloseProfile = () => {
    setProfileUser(null);
    // Also clear selection when closing profile
    setSelectedUsers([]);
  };

  const handleViewProfile = (user: UserDetails | null) => {
    setProfileUser(user);
  };

  const handleChangeRole = (user: UserDetails) => {
    setUserToChangeRole(user);
  };

  const handleDeleteUser = (user: UserDetails) => {
    setUserToDelete(user);
  };

  return (
    <>
      <SharedSideMenu ariaLabel={t`Toggle collapsed menu`} />
      <AppLayout
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
        <div className={`flex h-full ${profileUser ? "gap-0" : ""}`}>
          {/* Main content */}
          <div className={`flex min-w-0 flex-1 flex-col ${profileUser ? "sm:max-w-[calc(100%-21rem)]" : ""}`}>
            <h1>
              <Trans>Users</Trans>
            </h1>
            <p>
              <Trans>Manage your users and permissions here.</Trans>
            </p>

            <div className={`${profileUser ? "pr-4" : ""}`}>
              <UserToolbar selectedUsers={selectedUsers} onSelectedUsersChange={setSelectedUsers} />
            </div>
            <div className={`min-h-0 flex-1 ${profileUser ? "pr-4" : ""}`}>
              <UserTable
                selectedUsers={selectedUsers}
                onSelectedUsersChange={setSelectedUsers}
                onViewProfile={handleViewProfile}
                onChangeRole={handleChangeRole}
                onDeleteUser={handleDeleteUser}
              />
            </div>
          </div>

          {/* Side pane - always dock on sm+ */}
          {profileUser && (
            <div className="hidden sm:block sm:w-80 sm:flex-shrink-0">
              <UserProfileSidePane
                user={profileUser}
                isOpen={profileUser !== null}
                onClose={handleCloseProfile}
                onChangeRole={handleChangeRole}
                onDeleteUser={handleDeleteUser}
              />
            </div>
          )}
        </div>
      </AppLayout>

      {/* Side pane for mobile screens - overlay on mobile only */}
      <div className="sm:hidden">
        <UserProfileSidePane
          user={profileUser}
          isOpen={profileUser !== null}
          onClose={handleCloseProfile}
          onChangeRole={handleChangeRole}
          onDeleteUser={handleDeleteUser}
        />
      </div>

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
