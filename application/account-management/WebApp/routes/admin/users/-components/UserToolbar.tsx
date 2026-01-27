import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Button } from "@repo/ui/components/Button";
import { PlusIcon, Trash2Icon } from "lucide-react";
import { useEffect, useRef, useState } from "react";
import type { components } from "@/shared/lib/api/client";
import { api, UserRole } from "@/shared/lib/api/client";
import { DeleteUserDialog } from "./DeleteUserDialog";
import InviteUserDialog from "./InviteUserDialog";
import { TenantNameRequiredDialog } from "./TenantNameRequiredDialog";
import { UserQuerying } from "./UserQuerying";

type UserDetails = components["schemas"]["UserDetails"];

interface UserToolbarProps {
  selectedUsers: UserDetails[];
  onSelectedUsersChange: (users: UserDetails[]) => void;
}

// Thresholds based on max content widths (Danish language, long dates)
// Max expanded filters: 838px, Collapsed filters: 292px, Button with text: 136px, Gap: 8px
const THRESHOLD_BOTH_EXPANDED = 982; // 838 + 136 + 8: filters + button text
const THRESHOLD_BUTTON_EXPANDED = 438; // 292 + 138 + 8: collapsed filters + button text
const HYSTERESIS = 2;

export function UserToolbar({ selectedUsers, onSelectedUsersChange }: Readonly<UserToolbarProps>) {
  const { data: currentUser } = api.useQuery("get", "/api/account-management/users/me");
  const { data: tenant } = api.useQuery("get", "/api/account-management/tenants/current");
  const [isInviteModalOpen, setIsInviteModalOpen] = useState(false);
  const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false);
  const [showTenantNameRequiredDialog, setShowTenantNameRequiredDialog] = useState(false);
  const [showButtonText, setShowButtonText] = useState(true);
  const [areFiltersExpanded, setAreFiltersExpanded] = useState(false);
  const toolbarRef = useRef<HTMLDivElement>(null);

  const isOwner = currentUser?.role === UserRole.Owner;
  const hasSelectedSelf = selectedUsers.some((user) => user.id === currentUser?.id);
  const hasTenantName = tenant?.name && tenant.name.trim() !== "";

  const handleInviteClick = () => {
    if (!hasTenantName) {
      setShowTenantNameRequiredDialog(true);
      return;
    }
    setIsInviteModalOpen(true);
  };

  useEffect(() => {
    const toolbar = toolbarRef.current;
    if (!toolbar) {
      return;
    }

    const checkSpace = () => {
      const toolbarWidth = toolbar.offsetWidth;

      // Button text threshold depends on whether filters are expanded
      // If filters expanded: need room for both (980px)
      // If filters collapsed: need room for collapsed filters + button text (436px)
      const threshold = areFiltersExpanded ? THRESHOLD_BOTH_EXPANDED : THRESHOLD_BUTTON_EXPANDED;

      setShowButtonText((prev) => {
        if (prev && toolbarWidth < threshold - HYSTERESIS) {
          return false;
        }
        if (!prev && toolbarWidth >= threshold + HYSTERESIS) {
          return true;
        }
        return prev;
      });
    };

    const observer = new ResizeObserver(checkSpace);
    observer.observe(toolbar);
    checkSpace();

    return () => observer.disconnect();
  }, [areFiltersExpanded]);

  return (
    <div ref={toolbarRef} className="mb-4 flex items-center justify-between gap-2">
      <UserQuerying
        onFiltersUpdated={() => onSelectedUsersChange([])}
        onFiltersExpandedChange={setAreFiltersExpanded}
      />
      <div className="mt-auto flex items-center gap-2">
        {selectedUsers.length < 2 && isOwner && (
          <Button variant="default" onClick={handleInviteClick} aria-label={t`Invite user`}>
            <PlusIcon className="size-5" />
            {showButtonText && <Trans>Invite user</Trans>}
          </Button>
        )}
        {selectedUsers.length > 1 && isOwner && (
          <Button
            variant="destructive"
            onClick={() => setIsDeleteModalOpen(true)}
            disabled={hasSelectedSelf}
            aria-label={t`Delete ${selectedUsers.length} users`}
          >
            <Trash2Icon className="size-5" />
            {showButtonText && <Trans>Delete {selectedUsers.length} users</Trans>}
          </Button>
        )}
      </div>
      {isOwner && <InviteUserDialog isOpen={isInviteModalOpen} onOpenChange={setIsInviteModalOpen} />}
      <TenantNameRequiredDialog isOpen={showTenantNameRequiredDialog} onOpenChange={setShowTenantNameRequiredDialog} />
      <DeleteUserDialog
        users={selectedUsers}
        isOpen={isDeleteModalOpen}
        onOpenChange={setIsDeleteModalOpen}
        onUsersDeleted={() => onSelectedUsersChange([])}
      />
    </div>
  );
}
