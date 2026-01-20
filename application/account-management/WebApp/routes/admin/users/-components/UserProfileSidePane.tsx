import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { useUserInfo } from "@repo/infrastructure/auth/hooks";
import { Avatar, AvatarFallback, AvatarImage } from "@repo/ui/components/Avatar";
import { Badge } from "@repo/ui/components/Badge";
import { Button } from "@repo/ui/components/Button";
import { Separator } from "@repo/ui/components/Separator";
import { MEDIA_QUERIES } from "@repo/ui/utils/responsive";
import { formatDate } from "@repo/utils/date/formatDate";
import { getInitials } from "@repo/utils/string/getInitials";
import { InfoIcon, Trash2Icon, XIcon } from "lucide-react";
import type React from "react";
import { useEffect, useRef, useState } from "react";
import type { components } from "@/shared/lib/api/client";
import { getUserRoleLabel } from "@/shared/lib/api/userRole";
import { ChangeUserRoleDialog } from "./ChangeUserRoleDialog";

type UserDetails = components["schemas"]["UserDetails"];

interface UserProfileSidePaneProps {
  user: UserDetails | null;
  isOpen: boolean;
  onClose: () => void;
  onDeleteUser: (user: UserDetails) => void;
  isUserInCurrentView?: boolean;
  isDataNewer?: boolean;
  isLoading?: boolean;
}

function UserProfileContent({
  user,
  canModifyUser,
  onChangeRole
}: Readonly<{
  user: UserDetails;
  canModifyUser: boolean;
  onChangeRole: () => void;
}>) {
  return (
    <>
      {/* User Avatar and Basic Info */}
      <div className="mb-6 text-center">
        <Avatar className="mx-auto mb-3 size-16">
          <AvatarImage src={user.avatarUrl ?? undefined} />
          <AvatarFallback>{getInitials(user.firstName, user.lastName, user.email)}</AvatarFallback>
        </Avatar>
        <h4>
          {user.firstName} {user.lastName}
        </h4>
        {user.title && <span className="block text-muted-foreground text-sm">{user.title}</span>}
      </div>

      {/* Contact Information */}
      <div className="mb-4">
        <div className="space-y-2">
          <div className="flex items-start justify-between">
            <p className="text-sm">
              <Trans>Email</Trans>
            </p>
            <div className="flex flex-col items-end gap-1">
              <p className="text-right text-sm">{user.email}</p>
              {user.emailConfirmed ? (
                <Badge variant="secondary" className="bg-success text-success-foreground text-xs">
                  <Trans>Verified</Trans>
                </Badge>
              ) : (
                <Badge variant="outline" className="text-xs">
                  <Trans>Pending</Trans>
                </Badge>
              )}
            </div>
          </div>
        </div>
      </div>

      <Separator className="mb-4" />

      {/* Role Information */}
      <div className="mb-4 flex items-center justify-between">
        <span className="font-semibold text-sm">
          <Trans>Role</Trans>
        </span>
        {canModifyUser ? (
          <Button
            variant="ghost"
            className="h-auto p-0 text-xs"
            onClick={onChangeRole}
            aria-label={t`Change user role for ${`${user.firstName ?? ""} ${user.lastName ?? ""}`.trim() || user.email}`}
          >
            <Badge
              variant="outline"
              className="cursor-pointer text-xs transition-all duration-200 hover:scale-105 hover:bg-muted hover:shadow-sm"
            >
              {getUserRoleLabel(user.role)}
            </Badge>
          </Button>
        ) : (
          <Badge variant="outline" className="text-xs">
            {getUserRoleLabel(user.role)}
          </Badge>
        )}
      </div>

      <Separator className="mb-4" />

      {/* Account Details */}
      <div className="mb-4">
        <div className="space-y-4">
          <div className="flex justify-between">
            <p className="text-sm">
              <Trans>Created</Trans>
            </p>
            <p className="text-sm">{formatDate(user.createdAt, true)}</p>
          </div>
          <div className="flex justify-between">
            <p className="text-sm">
              <Trans>Last seen</Trans>
            </p>
            <p className="text-sm">{formatDate(user.lastSeenAt, true)}</p>
          </div>
        </div>
      </div>
    </>
  );
}

function useSidePaneAccessibility(
  isOpen: boolean,
  onClose: () => void,
  sidePaneRef: React.RefObject<HTMLDivElement | null>,
  _closeButtonRef: React.RefObject<SVGSVGElement | null>
) {
  const previouslyFocusedElement = useRef<HTMLElement | null>(null);

  useEffect(() => {
    const isSmallScreen = !window.matchMedia(MEDIA_QUERIES.md).matches;
    if (isOpen && isSmallScreen) {
      // Store the currently focused element before moving focus
      previouslyFocusedElement.current = document.activeElement as HTMLElement;
    }
  }, [isOpen]);

  // Prevent body scroll on small screens when side pane is open
  useEffect(() => {
    const isSmallScreen = !window.matchMedia(MEDIA_QUERIES.md).matches;
    if (isOpen && isSmallScreen) {
      const originalStyle = window.getComputedStyle(document.body).overflow;
      document.body.style.overflow = "hidden";
      return () => {
        document.body.style.overflow = originalStyle;
      };
    }
  }, [isOpen]);

  useEffect(() => {
    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape" && isOpen) {
        event.preventDefault();
        onClose();
      }
    };

    if (isOpen) {
      document.addEventListener("keydown", handleKeyDown);
    }

    return () => {
      document.removeEventListener("keydown", handleKeyDown);
    };
  }, [isOpen, onClose]);

  useEffect(() => {
    const isSmallScreen = !window.matchMedia(MEDIA_QUERIES.md).matches;
    if (!isOpen || !sidePaneRef.current || !isSmallScreen) {
      return;
    }

    const focusableElements = sidePaneRef.current.querySelectorAll(
      'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])'
    );
    const firstElement = focusableElements[0] as HTMLElement;
    const lastElement = focusableElements[focusableElements.length - 1] as HTMLElement;

    const handleTabKey = (event: KeyboardEvent) => {
      if (event.key !== "Tab") {
        return;
      }

      const isShiftTab = event.shiftKey;
      const activeElement = document.activeElement;

      if (isShiftTab && activeElement === firstElement) {
        event.preventDefault();
        lastElement.focus();
      } else if (!isShiftTab && activeElement === lastElement) {
        event.preventDefault();
        firstElement.focus();
      }
    };

    document.addEventListener("keydown", handleTabKey);
    return () => document.removeEventListener("keydown", handleTabKey);
  }, [isOpen, sidePaneRef]);
}

export function UserProfileSidePane({
  user,
  isOpen,
  onClose,
  onDeleteUser,
  isUserInCurrentView = true,
  isDataNewer = false,
  isLoading = false
}: Readonly<UserProfileSidePaneProps>) {
  const userInfo = useUserInfo();
  const sidePaneRef = useRef<HTMLDivElement>(null);
  const closeButtonRef = useRef<SVGSVGElement>(null);
  const [isChangeRoleDialogOpen, setIsChangeRoleDialogOpen] = useState(false);
  const [isSmallScreen, setIsSmallScreen] = useState(false);

  // Check screen size for backdrop rendering
  useEffect(() => {
    const checkScreenSize = () => {
      setIsSmallScreen(!window.matchMedia(MEDIA_QUERIES.md).matches);
    };

    checkScreenSize();
    window.addEventListener("resize", checkScreenSize);
    return () => window.removeEventListener("resize", checkScreenSize);
  }, []);

  useSidePaneAccessibility(isOpen, onClose, sidePaneRef, closeButtonRef);

  if (!isOpen) {
    return null;
  }

  const isCurrentUser = user?.id === userInfo?.id;
  const canModifyUser = userInfo?.role === "Owner" && !isCurrentUser;

  return (
    <>
      {/* Backdrop for small screens */}
      {isSmallScreen && <div className="fixed inset-0 z-[45] bg-black/50" aria-hidden="true" />}

      {/* Side pane */}
      <section
        ref={sidePaneRef}
        className={`relative flex h-full w-full flex-col border-border border-l ${isSmallScreen ? "fixed inset-0 z-[46] bg-card" : ""}`}
        aria-label={t`User profile`}
      >
        {/* Close button - positioned like modal dialogs */}
        <XIcon
          ref={closeButtonRef}
          onClick={() => onClose()}
          className="absolute top-3 right-2 z-10 size-10 cursor-pointer p-2 hover:bg-muted focus:bg-muted focus:outline-none focus-visible:ring-2 focus-visible:ring-ring"
          aria-label={t`Close user profile`}
          tabIndex={-1}
        />

        <div className="h-16 border-border border-t border-b bg-muted/30 px-4 py-8 backdrop-blur-sm">
          <h4 className="flex h-full items-center">
            <Trans>User profile</Trans>
          </h4>
        </div>

        {/* Notice when user is not in current filtered view - only show on desktop with pagination */}
        {!isUserInCurrentView && !isSmallScreen && (
          <div className="border-border border-b bg-muted px-4 py-3">
            <div className="flex items-center gap-2 text-muted-foreground">
              <InfoIcon className="size-4 flex-shrink-0" />
              <p className="font-medium text-sm">
                <Trans>User not in current view</Trans>
              </p>
            </div>
          </div>
        )}

        {/* Notice when data is different from table */}
        {isDataNewer && (
          <div className="border-border border-b bg-muted px-4 py-3">
            <div className="flex items-center gap-2 text-muted-foreground">
              <InfoIcon className="size-4 flex-shrink-0" />
              <p className="font-medium text-sm">
                <Trans>User data updated</Trans>
              </p>
            </div>
          </div>
        )}

        {/* Content */}
        <div className="flex-1 overflow-y-auto">
          {isLoading ? (
            <div className="p-4">
              {/* Avatar skeleton matching exact position */}
              <div className="mb-6 text-center">
                <div className="mx-auto mb-3 size-20 animate-pulse rounded-full bg-muted" />
                <div className="mx-auto mb-2 h-6 w-32 animate-pulse rounded bg-muted" />
                <div className="mx-auto h-4 w-24 animate-pulse rounded bg-muted" />
              </div>

              {/* Single block for all other content */}
              <div className="h-64 w-full animate-pulse rounded-lg bg-muted" />
            </div>
          ) : (
            user && (
              <div className="p-4">
                <UserProfileContent
                  user={user}
                  canModifyUser={canModifyUser}
                  onChangeRole={() => setIsChangeRoleDialogOpen(true)}
                />
              </div>
            )
          )}
        </div>

        {/* Quick Actions */}
        {userInfo?.role === "Owner" && user && (
          <div className="relative mt-auto border-border border-t p-4 pb-[max(1rem,env(safe-area-inset-bottom))]">
            <Button
              variant="destructive"
              onClick={() => onDeleteUser(user)}
              className="w-full justify-center text-sm"
              disabled={isCurrentUser}
            >
              <Trash2Icon className="size-4" />
              <Trans>Delete user</Trans>
            </Button>
          </div>
        )}
      </section>

      {/* Change User Role Dialog */}
      {user && (
        <ChangeUserRoleDialog user={user} isOpen={isChangeRoleDialogOpen} onOpenChange={setIsChangeRoleDialogOpen} />
      )}
    </>
  );
}
