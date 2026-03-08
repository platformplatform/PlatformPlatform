import type { useUserInfo } from "@repo/infrastructure/auth/hooks";

import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { DropdownMenuGroup, DropdownMenuItem, DropdownMenuSeparator } from "@repo/ui/components/DropdownMenu";
import { ArrowLeftIcon, LogOutIcon, MailQuestion, SettingsIcon, SlidersHorizontalIcon } from "lucide-react";

import type { TenantInfo } from "../common/tenantUtils";

import { TenantSwitcher } from "./TenantSwitcher";
import { UserProfileCard } from "./UserProfileCard";

interface UserMenuDropdownContentProps {
  userInfo: NonNullable<ReturnType<typeof useUserInfo>>;
  isAccountContext: boolean;
  canAccessAccountSettings: boolean;
  sortedTenants: TenantInfo[];
  currentTenantId: string | undefined;
  isLoadingTenants: boolean;
  onNavigateBackToApp: () => void;
  onNavigateToProfile: () => void;
  onNavigateToPreferences: () => void;
  onNavigateToAccountSettings: () => void;
  onLogout: () => void;
  onShowSupport: () => void;
  onTenantSwitch: (tenant: TenantInfo) => void;
}

export function UserMenuDropdownContent({
  userInfo,
  isAccountContext,
  canAccessAccountSettings,
  sortedTenants,
  currentTenantId,
  isLoadingTenants,
  onNavigateBackToApp,
  onNavigateToProfile,
  onNavigateToPreferences,
  onNavigateToAccountSettings,
  onLogout,
  onShowSupport,
  onTenantSwitch
}: Readonly<UserMenuDropdownContentProps>) {
  return (
    <>
      {isAccountContext && (
        <>
          <DropdownMenuItem onClick={onNavigateBackToApp} aria-label={t`Back to app`}>
            <ArrowLeftIcon className="size-5" />
            <Trans>Back to app</Trans>
          </DropdownMenuItem>
          <DropdownMenuSeparator />
        </>
      )}

      <UserProfileCard userInfo={userInfo} onNavigateToProfile={onNavigateToProfile} />

      <DropdownMenuItem onClick={onNavigateToPreferences} aria-label={t`Change user preferences`}>
        <SlidersHorizontalIcon className="size-5" />
        <Trans>Preferences</Trans>
      </DropdownMenuItem>

      <DropdownMenuItem onClick={onLogout} aria-label={t`Log out`}>
        <LogOutIcon className="size-5" />
        <Trans>Log out</Trans>
      </DropdownMenuItem>

      {(canAccessAccountSettings || sortedTenants.length > 1) && <DropdownMenuSeparator />}

      {(canAccessAccountSettings || sortedTenants.length > 1) && (
        <DropdownMenuGroup>
          <TenantSwitcher
            sortedTenants={sortedTenants}
            currentTenantId={currentTenantId}
            isLoadingTenants={isLoadingTenants}
            userInfo={userInfo}
            onTenantSwitch={onTenantSwitch}
          />
          {canAccessAccountSettings && (
            <DropdownMenuItem onClick={onNavigateToAccountSettings} aria-label={t`Account settings`}>
              <SettingsIcon className="size-5" />
              <Trans>Account settings</Trans>
            </DropdownMenuItem>
          )}
        </DropdownMenuGroup>
      )}

      <DropdownMenuSeparator />

      <DropdownMenuItem onClick={onShowSupport} aria-label={t`Contact support`}>
        <MailQuestion className="size-5" />
        <Trans>Contact support</Trans>
      </DropdownMenuItem>
    </>
  );
}
