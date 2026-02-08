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
// Measured at 16px base: filters 840px (52.5rem), button with text 137px (8.5625rem), gap 8px (0.5rem)
const THRESHOLD_BOTH_EXPANDED_REM = 61.5; // 52.5 + 8.5625 + 0.5: filters + button with text
const THRESHOLD_BUTTON_EXPANDED_REM = 24; // collapsed filters (~15rem) + button with text + gap
const HYSTERESIS_REM = 0.14;

function getRemInPixels(): number {
  return parseFloat(getComputedStyle(document.documentElement).fontSize);
}

function getThresholdBothExpanded(): number {
  return THRESHOLD_BOTH_EXPANDED_REM * getRemInPixels();
}

function getThresholdButtonExpanded(): number {
  return THRESHOLD_BUTTON_EXPANDED_REM * getRemInPixels();
}

function getHysteresis(): number {
  return HYSTERESIS_REM * getRemInPixels();
}

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
      // Thresholds scale with font size via rem conversion
      const threshold = areFiltersExpanded ? getThresholdBothExpanded() : getThresholdButtonExpanded();
      const hysteresis = getHysteresis();

      setShowButtonText((prev) => {
        if (prev && toolbarWidth < threshold - hysteresis) {
          return false;
        }
        if (!prev && toolbarWidth >= threshold + hysteresis) {
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
