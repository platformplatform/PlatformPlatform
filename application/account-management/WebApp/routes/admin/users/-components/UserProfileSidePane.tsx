import type { components } from "@/shared/lib/api/client";
import { getUserRoleLabel } from "@/shared/lib/api/userRole";
import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { useUserInfo } from "@repo/infrastructure/auth/hooks";
import { Avatar } from "@repo/ui/components/Avatar";
import { Badge } from "@repo/ui/components/Badge";
import { Button } from "@repo/ui/components/Button";
import { Heading } from "@repo/ui/components/Heading";
import { Separator } from "@repo/ui/components/Separator";
import { Text } from "@repo/ui/components/Text";
import { formatDate } from "@repo/utils/date/formatDate";
import { getInitials } from "@repo/utils/string/getInitials";
import { InfoIcon, Trash2Icon, XIcon } from "lucide-react";
import { useEffect, useRef, useState } from "react";
import { ChangeUserRoleDialog } from "./ChangeUserRoleDialog";

type UserDetails = components["schemas"]["UserDetails"];

interface UserProfileSidePaneProps {
  user: UserDetails | null;
  isOpen: boolean;
  onClose: () => void;
  onDeleteUser: (user: UserDetails) => void;
  isUserInCurrentView?: boolean;
}

export function UserProfileSidePane({
  user,
  isOpen,
  onClose,
  onDeleteUser,
  isUserInCurrentView = true
}: Readonly<UserProfileSidePaneProps>) {
  const userInfo = useUserInfo();
  const sidePaneRef = useRef<HTMLDivElement>(null);
  const closeButtonRef = useRef<SVGSVGElement>(null);
  const [isChangeRoleDialogOpen, setIsChangeRoleDialogOpen] = useState(false);

  // Focus management and keyboard navigation - only focus close button on mobile
  useEffect(() => {
    if (isOpen && closeButtonRef.current) {
      // Only auto-focus on mobile, not on larger screens where it's part of the layout
      const isMobileScreen = window.matchMedia("(max-width: 639px)").matches;
      if (isMobileScreen) {
        closeButtonRef.current.focus();
      }
    }
  }, [isOpen]);

  // Escape key handler
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

  // Focus trapping - only on mobile, not on larger screens where side pane is part of layout
  useEffect(() => {
    if (!isOpen || !sidePaneRef.current) {
      return;
    }

    // Don't trap focus on larger screens where side pane is part of main layout
    const isMobileScreen = window.matchMedia("(max-width: 639px)").matches;
    if (!isMobileScreen) {
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

    return () => {
      document.removeEventListener("keydown", handleTabKey);
    };
  }, [isOpen]);

  if (!isOpen) {
    return null;
  }

  const isCurrentUser = user?.id === userInfo?.id;
  const canModifyUser = userInfo?.role === "Owner" && !isCurrentUser;

  return (
    <>
      {/* Side pane */}
      <div
        ref={sidePaneRef}
        className="relative flex h-full w-full flex-col border-border border-l bg-background"
        role="complementary"
        aria-label={t`User profile details`}
      >
        {/* Close button - positioned like modal dialogs */}
        <XIcon
          ref={closeButtonRef}
          onClick={onClose}
          onKeyDown={(e) => {
            if (e.key === "Enter" || e.key === " ") {
              e.preventDefault();
              onClose();
            }
          }}
          tabIndex={0}
          role="button"
          className="absolute top-3 right-2 z-10 h-10 w-10 cursor-pointer p-2 hover:bg-muted focus:bg-muted focus:outline-none focus-visible:ring-2 focus-visible:ring-ring"
          aria-label={t`Close user profile`}
        />

        <div className="h-16 border-border border-t border-b bg-muted/30 px-4 py-8 backdrop-blur-sm">
          <Heading level={2} className="flex h-full items-center font-semibold text-base">
            <Trans>User profile</Trans>
          </Heading>
        </div>

        {/* Notice when user is not in current filtered view */}
        {!isUserInCurrentView && (
          <div className="border-border border-b bg-muted px-4 py-3">
            <div className="flex items-center gap-2 text-muted-foreground">
              <InfoIcon className="h-4 w-4 flex-shrink-0" />
              <Text className="font-medium text-sm">
                <Trans>User not in current view</Trans>
              </Text>
            </div>
          </div>
        )}

        {/* Content */}
        <div className="flex-1 overflow-y-auto">
          {user && (
            <div className="p-4">
              <>
                {/* User Avatar and Basic Info */}
                <div className="mb-6 text-center">
                  <Avatar
                    initials={getInitials(user.firstName, user.lastName, user.email)}
                    avatarUrl={user.avatarUrl}
                    size="lg"
                    isRound={true}
                    className="mx-auto mb-3"
                  />
                  <Heading level={3} className="font-semibold text-lg">
                    {user.firstName} {user.lastName}
                  </Heading>
                  {user.title && <Text className="text-muted-foreground text-sm">{user.title}</Text>}
                </div>

                {/* Contact Information */}
                <div className="mb-4">
                  <div className="space-y-2">
                    <div className="flex items-start justify-between">
                      <Text className="text-sm">
                        <Trans>Email</Trans>
                      </Text>
                      <div className="flex flex-col items-end gap-1">
                        <Text className="text-right text-sm">{user.email}</Text>
                        {user.emailConfirmed ? (
                          <Badge variant="success" className="text-xs">
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
                  <Heading level={4} className="font-medium text-sm">
                    <Trans>Role</Trans>
                  </Heading>
                  {canModifyUser ? (
                    <Button
                      variant="ghost"
                      className="h-auto p-0 text-xs"
                      onPress={() => setIsChangeRoleDialogOpen(true)}
                      aria-label={t`Change user role for ${user.firstName} ${user.lastName}`}
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
                      <Text className="text-sm">
                        <Trans>Created</Trans>
                      </Text>
                      <Text className="text-sm">{formatDate(user.createdAt, true)}</Text>
                    </div>
                    <div className="flex justify-between">
                      <Text className="text-sm">
                        <Trans>Modified</Trans>
                      </Text>
                      <Text className="text-sm">{formatDate(user.modifiedAt, true)}</Text>
                    </div>
                  </div>
                </div>
              </>
            </div>
          )}
        </div>

        {/* Quick Actions */}
        {canModifyUser && user && (
          <div className="p-4">
            <Button variant="destructive" onPress={() => onDeleteUser(user)} className="w-full justify-center text-sm">
              <Trash2Icon className="h-4 w-4" />
              <Trans>Delete user</Trans>
            </Button>
          </div>
        )}
      </div>

      {/* Change User Role Dialog */}
      {user && (
        <ChangeUserRoleDialog user={user} isOpen={isChangeRoleDialogOpen} onOpenChange={setIsChangeRoleDialogOpen} />
      )}
    </>
  );
}
