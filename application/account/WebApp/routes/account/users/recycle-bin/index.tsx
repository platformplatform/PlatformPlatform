import { t } from "@lingui/core/macro";
import { hasPermission } from "@repo/infrastructure/auth/routeGuards";
import { AppLayout } from "@repo/ui/components/AppLayout";
import { createFileRoute } from "@tanstack/react-router";
import { useState } from "react";
import FederatedAccessDeniedPage from "@/federated-modules/errorPages/FederatedAccessDeniedPage";
import FederatedSideMenu from "@/federated-modules/sideMenu/FederatedSideMenu";
import type { components } from "@/shared/lib/api/client";
import { UserTabNavigation } from "../-components/UserTabNavigation";
import { DeletedUsersTable } from "./-components/DeletedUsersTable";
import { DeletedUsersToolbar } from "./-components/DeletedUsersToolbar";
import { PermanentlyDeleteUserDialog } from "./-components/PermanentlyDeleteUserDialog";

type DeletedUserDetails = components["schemas"]["DeletedUserDetails"];

export const Route = createFileRoute("/account/users/recycle-bin/")({
  component: DeletedUsersPage
});

export default function DeletedUsersPage() {
  const [selectedDeletedUsers, setSelectedDeletedUsers] = useState<DeletedUserDetails[]>([]);
  const [usersToDelete, setUsersToDelete] = useState<DeletedUserDetails[]>([]);
  const [isEmptyRecycleBin, setIsEmptyRecycleBin] = useState(false);
  const [totalDeletedUsersCount, setTotalDeletedUsersCount] = useState(0);
  const [pageOffset, setPageOffset] = useState(0);

  if (!hasPermission({ allowedRoles: ["Owner", "Admin"] })) {
    return <FederatedAccessDeniedPage />;
  }

  const handlePageChange = (page: number) => {
    setPageOffset(page - 1);
    setSelectedDeletedUsers([]);
  };

  const handlePermanentlyDeleteUsers = (users: DeletedUserDetails[]) => {
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
      <FederatedSideMenu currentSystem="account" />
      <AppLayout title={t`Users`} subtitle={t`Manage your users and permissions here.`} scrollAwayHeader={true}>
        <div className="flex min-h-0 flex-1 flex-col">
          <UserTabNavigation activeTab="recycle-bin" />
          <DeletedUsersToolbar
            selectedUsers={selectedDeletedUsers}
            onSelectedUsersChange={setSelectedDeletedUsers}
            onPermanentlyDelete={handlePermanentlyDeleteUsers}
            onEmptyRecycleBin={handleEmptyRecycleBin}
          />
          <DeletedUsersTable
            selectedUsers={selectedDeletedUsers}
            onSelectedUsersChange={setSelectedDeletedUsers}
            pageOffset={pageOffset}
            onPageChange={handlePageChange}
          />
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
