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
import { PencilIcon, Trash2Icon, XIcon } from "lucide-react";
import { useEffect, useRef } from "react";

type UserDetails = components["schemas"]["UserDetails"];

interface UserProfileSidePaneProps {
  user: UserDetails | null;
  isOpen: boolean;
  onClose: () => void;
  onChangeRole: (user: UserDetails) => void;
  onDeleteUser: (user: UserDetails) => void;
}

export function UserProfileSidePane({
  user,
  isOpen,
  onClose,
  onChangeRole,
  onDeleteUser
}: Readonly<UserProfileSidePaneProps>) {
  const userInfo = useUserInfo();
  const sidePaneRef = useRef<HTMLDivElement>(null);
  const closeButtonRef = useRef<HTMLButtonElement>(null);

  // Focus management and keyboard navigation - only focus close button on mobile/tablet
  useEffect(() => {
    if (isOpen && closeButtonRef.current) {
      // Only auto-focus on mobile/tablet, not on 2xl desktop where it's part of the layout
      const is2xlScreen = window.matchMedia("(min-width: 1536px)").matches;
      if (!is2xlScreen) {
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

  // Focus trapping - only on mobile/tablet, not on 2xl desktop
  useEffect(() => {
    if (!isOpen || !sidePaneRef.current) {
      return;
    }

    // Don't trap focus on 2xl screens where side pane is part of main layout
    const is2xlScreen = window.matchMedia("(min-width: 1536px)").matches;
    if (is2xlScreen) {
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

  if (!isOpen || !user) {
    return null;
  }

  const isCurrentUser = user.id === userInfo?.id;
  const canModifyUser = userInfo?.role === "Owner" && !isCurrentUser;

  return (
    <>
      {/* Backdrop for tablet/mobile - only show when not in 2xl layout */}
      {isOpen && (
        <div
          className="fixed inset-0 z-40 bg-black/20 2xl:hidden"
          onMouseDown={onClose}
          onKeyDown={(e) => {
            if (e.key === "Enter" || e.key === " ") {
              onClose();
            }
          }}
          aria-label={t`Close user profile`}
          role="button"
          tabIndex={0}
        />
      )}

      {/* Side pane */}
      <div
        ref={sidePaneRef}
        className="fixed inset-y-0 left-0 z-50 flex w-full flex-col border-border border-r bg-background shadow-xl transition-transform duration-300 ease-in-out sm:w-96 2xl:static 2xl:z-auto 2xl:h-full 2xl:w-full 2xl:border-r 2xl:shadow-none"
        role="complementary"
        aria-label={t`User profile details`}
      >
        {/* Header */}
        <div className="flex items-center justify-between border-border border-b p-4">
          <Heading level={2} className="font-semibold text-lg">
            <Trans>User profile</Trans>
          </Heading>
          <Button
            ref={closeButtonRef}
            variant="icon"
            onPress={onClose}
            aria-label={t`Close user profile`}
            className="2xl:hidden"
          >
            <XIcon className="h-5 w-5" />
          </Button>
        </div>

        {/* Content */}
        <div className="flex-1 overflow-y-auto p-4">
          {/* User Avatar and Basic Info */}
          <div className="mb-6 text-center">
            <Avatar
              initials={getInitials(user.firstName, user.lastName, user.email)}
              avatarUrl={user.avatarUrl}
              size="lg"
              isRound={true}
              className="mx-auto mb-4"
            />
            <Heading level={3} className="font-semibold text-xl">
              {user.firstName} {user.lastName}
            </Heading>
            {user.title && <Text className="text-muted-foreground">{user.title}</Text>}
          </div>

          {/* Contact Information */}
          <div className="mb-6">
            <Heading level={4} className="mb-3 font-medium text-muted-foreground text-sm uppercase tracking-wide">
              <Trans>Contact</Trans>
            </Heading>
            <div className="space-y-2">
              <div className="flex items-center justify-between">
                <Text className="font-medium">
                  <Trans>Email</Trans>
                </Text>
                <div className="flex items-center gap-2">
                  <Text className="text-right">{user.email}</Text>
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

          <Separator className="mb-6" />

          {/* Role Information */}
          <div className="mb-6">
            <Heading level={4} className="mb-3 font-medium text-muted-foreground text-sm uppercase tracking-wide">
              <Trans>Role</Trans>
            </Heading>
            <Badge variant="outline">{getUserRoleLabel(user.role)}</Badge>
          </div>

          <Separator className="mb-6" />

          {/* Account Details */}
          <div className="mb-6">
            <Heading level={4} className="mb-3 font-medium text-muted-foreground text-sm uppercase tracking-wide">
              <Trans>Account details</Trans>
            </Heading>
            <div className="space-y-3">
              <div className="flex justify-between">
                <Text className="text-muted-foreground">
                  <Trans>Created</Trans>
                </Text>
                <Text>{formatDate(user.createdAt)}</Text>
              </div>
              <div className="flex justify-between">
                <Text className="text-muted-foreground">
                  <Trans>Modified</Trans>
                </Text>
                <Text>{formatDate(user.modifiedAt)}</Text>
              </div>
            </div>
          </div>

          <Separator className="mb-6" />

          {/* Future Extensions Placeholders */}
          <div className="mb-6">
            <Heading level={4} className="mb-3 font-medium text-muted-foreground text-sm uppercase tracking-wide">
              <Trans>Timezone</Trans>
            </Heading>
            <Text className="text-muted-foreground">
              <Trans>Not set</Trans>
            </Text>
          </div>

          <Separator className="mb-6" />

          <div className="mb-6">
            <Heading level={4} className="mb-3 font-medium text-muted-foreground text-sm uppercase tracking-wide">
              <Trans>Recent login history</Trans>
            </Heading>
            <Text className="text-muted-foreground">
              <Trans>No recent activity</Trans>
            </Text>
          </div>

          <Separator className="mb-6" />

          <div className="mb-6">
            <Heading level={4} className="mb-3 font-medium text-muted-foreground text-sm uppercase tracking-wide">
              <Trans>Team memberships</Trans>
            </Heading>
            <Text className="text-muted-foreground">
              <Trans>No team memberships</Trans>
            </Text>
          </div>
        </div>

        {/* Quick Actions */}
        {canModifyUser && (
          <div className="border-border border-t p-4">
            <Heading level={4} className="mb-3 font-medium text-muted-foreground text-sm uppercase tracking-wide">
              <Trans>Quick actions</Trans>
            </Heading>
            <div className="space-y-2">
              <Button variant="outline" onPress={() => onChangeRole(user)} className="w-full justify-start">
                <PencilIcon className="h-4 w-4" />
                <Trans>Change role</Trans>
              </Button>
              <Button variant="destructive" onPress={() => onDeleteUser(user)} className="w-full justify-start">
                <Trash2Icon className="h-4 w-4" />
                <Trans>Delete user</Trans>
              </Button>
            </div>
          </div>
        )}
      </div>
    </>
  );
}
