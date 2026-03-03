import { t } from "@lingui/core/macro";
import { requirePermission } from "@repo/infrastructure/auth/routeGuards";
import type { useDeletedUsers } from "@repo/infrastructure/sync/hooks";
import { AppLayout } from "@repo/ui/components/AppLayout";
import { createFileRoute } from "@tanstack/react-router";
import { useState } from "react";
import { UserTabNavigation } from "../-components/UserTabNavigation";
import { DeletedUsersTable } from "./-components/DeletedUsersTable";
import { DeletedUsersToolbar } from "./-components/DeletedUsersToolbar";
import { PermanentlyDeleteUserDialog } from "./-components/PermanentlyDeleteUserDialog";

type ElectricDeletedUser = ReturnType<typeof useDeletedUsers>["data"][number];

export const Route = createFileRoute("/account/users/recycle-bin/")({
  staticData: { trackingTitle: "User recycle bin" },
  beforeLoad: () => requirePermission({ allowedRoles: ["Owner", "Admin"] }),
  component: DeletedUsersPage
});

export default function DeletedUsersPage() {
  const [selectedDeletedUsers, setSelectedDeletedUsers] = useState<ElectricDeletedUser[]>([]);
  const [usersToDelete, setUsersToDelete] = useState<ElectricDeletedUser[]>([]);
  const [isEmptyRecycleBin, setIsEmptyRecycleBin] = useState(false);
  const [totalDeletedUsersCount, setTotalDeletedUsersCount] = useState(0);

  const handlePermanentlyDeleteUsers = (users: ElectricDeletedUser[]) => {
    setIsEmptyRecycleBin(false);
    setUsersToDelete(users);
  };

  const handleEmptyRecycleBin = (totalCount: number) => {
    setTotalDeletedUsersCount(totalCount);
    setIsEmptyRecycleBin(true);
    setUsersToDelete([]);
  };

  return (
    <>
      <AppLayout
        variant="center"
        maxWidth="64rem"
        title={t`User recycle bin`}
        subtitle={t`Manage your users and permissions here.`}
      >
        <UserTabNavigation activeTab="recycle-bin" />
        <div className="flex min-h-0 flex-1 flex-col">
          <DeletedUsersToolbar
            selectedUsers={selectedDeletedUsers}
            onSelectedUsersChange={setSelectedDeletedUsers}
            onPermanentlyDelete={handlePermanentlyDeleteUsers}
            onEmptyRecycleBin={handleEmptyRecycleBin}
          />
          <DeletedUsersTable selectedUsers={selectedDeletedUsers} onSelectedUsersChange={setSelectedDeletedUsers} />
        </div>
      </AppLayout>

      <PermanentlyDeleteUserDialog
        users={usersToDelete}
        isEmptyRecycleBin={isEmptyRecycleBin}
        totalDeletedUsersCount={totalDeletedUsersCount}
        isOpen={usersToDelete.length > 0 || isEmptyRecycleBin}
        onOpenChange={(isOpen) => {
          if (!isOpen) {
            setUsersToDelete([]);
            setIsEmptyRecycleBin(false);
          }
        }}
        onUsersDeleted={() => {
          setSelectedDeletedUsers([]);
        }}
      />
    </>
  );
}
