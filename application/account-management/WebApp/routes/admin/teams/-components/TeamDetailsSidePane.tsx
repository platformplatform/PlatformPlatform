import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { useUserInfo } from "@repo/infrastructure/auth/hooks";
import { Avatar } from "@repo/ui/components/Avatar";
import { Badge } from "@repo/ui/components/Badge";
import { Button } from "@repo/ui/components/Button";
import { Heading } from "@repo/ui/components/Heading";
import { Separator } from "@repo/ui/components/Separator";
import { Text } from "@repo/ui/components/Text";
import { MEDIA_QUERIES } from "@repo/ui/utils/responsive";
import { getInitials } from "@repo/utils/string/getInitials";
import { EditIcon, Trash2Icon, UsersIcon, XIcon } from "lucide-react";
import type React from "react";
import { useEffect, useRef, useState } from "react";
import { api, type components } from "@/shared/lib/api/client";
import { mockTeamMembers, type TeamMemberDetails } from "../-data/mockTeamMembers";
import type { TeamDetails } from "../-data/mockTeams";
import { EditTeamMembersDialog } from "./EditTeamMembersDialog";

type TeamSummary = components["schemas"]["TeamSummary"];
type TeamResponse = components["schemas"]["TeamResponse"];

interface TeamDetailsSidePaneProps {
  team: TeamSummary | null;
  isOpen: boolean;
  onClose: () => void;
  onEditTeam: () => void;
  onDeleteTeam: () => void;
}

function TeamDetailsContent({
  team,
  members,
  canViewMembers,
  canEditMembers,
  onEditMembers,
  isLoadingMembers
}: Readonly<{
  team: TeamResponse;
  members: TeamMemberDetails[];
  canViewMembers: boolean;
  canEditMembers: boolean;
  onEditMembers: () => void;
  isLoadingMembers: boolean;
}>) {
  const sortedMembers = [...members].sort((a, b) => {
    if (a.role === b.role) {
      return a.name.localeCompare(b.name);
    }
    return a.role === "Admin" ? -1 : 1;
  });

  return (
    <>
      <div className="mb-6">
        <Heading level={3} className="mb-2 font-semibold text-lg">
          {team.name}
        </Heading>
        <Text className="text-muted-foreground text-sm">{team.description}</Text>
      </div>

      <Separator className="mb-4" />

      <div className="mb-4">
        <div className="mb-3 flex items-center justify-between">
          <Heading level={4} className="font-medium text-sm">
            <Trans>Members</Trans> ({members.length})
          </Heading>
          {canEditMembers && (
            <Button variant="ghost" className="h-auto p-0 text-xs" onPress={onEditMembers}>
              <EditIcon className="h-3 w-3" />
              <Trans>Edit Members</Trans>
            </Button>
          )}
        </div>

        {!canViewMembers ? (
          <Text className="text-muted-foreground text-sm">
            <Trans>You must be a team member to view members</Trans>
          </Text>
        ) : isLoadingMembers ? (
          <div className="space-y-3">
            {["skeleton-1", "skeleton-2", "skeleton-3"].map((skeletonId) => (
              <div key={skeletonId} className="flex items-center gap-3">
                <div className="h-8 w-8 animate-pulse rounded-full bg-muted" />
                <div className="min-w-0 flex-1 space-y-2">
                  <div className="flex items-center gap-2">
                    <div className="h-4 w-32 animate-pulse rounded bg-muted" />
                    <div className="h-5 w-16 animate-pulse rounded bg-muted" />
                  </div>
                  <div className="h-3 w-40 animate-pulse rounded bg-muted" />
                  <div className="h-3 w-28 animate-pulse rounded bg-muted" />
                </div>
              </div>
            ))}
          </div>
        ) : members.length === 0 ? (
          <div className="flex flex-col items-center py-8 text-center">
            <UsersIcon className="mb-3 h-12 w-12 text-muted-foreground/50" />
            <Text className="mb-1 font-medium text-sm">
              <Trans>No members yet</Trans>
            </Text>
            <Text className="text-muted-foreground text-xs">
              <Trans>Add members to get started</Trans>
            </Text>
          </div>
        ) : (
          <div className="space-y-3">
            {sortedMembers.map((member) => (
              <div key={member.id} className="flex items-center gap-3">
                <Avatar
                  initials={getInitials(member.name.split(" ")[0], member.name.split(" ")[1], member.email)}
                  avatarUrl={member.avatarUrl}
                  size="sm"
                  isRound={true}
                />
                <div className="min-w-0 flex-1">
                  <div className="flex items-center gap-2">
                    <Text className="truncate font-medium text-sm">{member.name}</Text>
                    <Badge variant={member.role === "Admin" ? "primary" : "outline"} className="text-xs">
                      <Trans>{member.role}</Trans>
                    </Badge>
                  </div>
                  <Text className="truncate text-muted-foreground text-xs">{member.email}</Text>
                  {member.title && <Text className="truncate text-muted-foreground text-xs">{member.title}</Text>}
                </div>
              </div>
            ))}
          </div>
        )}
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
      previouslyFocusedElement.current = document.activeElement as HTMLElement;
    }
  }, [isOpen]);

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

export function TeamDetailsSidePane({
  team,
  isOpen,
  onClose,
  onEditTeam,
  onDeleteTeam
}: Readonly<TeamDetailsSidePaneProps>) {
  const userInfo = useUserInfo();
  const sidePaneRef = useRef<HTMLDivElement>(null);
  const closeButtonRef = useRef<SVGSVGElement>(null);
  const [isSmallScreen, setIsSmallScreen] = useState(false);
  const [isEditMembersDialogOpen, setIsEditMembersDialogOpen] = useState(false);
  const [teamMembers, setTeamMembers] = useState<TeamMemberDetails[]>([]);
  const [isLoadingMembers, setIsLoadingMembers] = useState(false);

  const {
    data: teamDetails,
    isLoading,
    error
  } = api.useQuery("get", "/api/account-management/teams/{id}", {
    params: {
      path: {
        id: team?.id || ""
      }
    },
    enabled: !!team?.id
  });

  useEffect(() => {
    if (team?.id) {
      setIsLoadingMembers(true);
      const timer = setTimeout(() => {
        setTeamMembers(mockTeamMembers[team.id] || []);
        setIsLoadingMembers(false);
      }, 500);
      return () => clearTimeout(timer);
    }
  }, [team?.id]);

  const isUserTeamMember = teamMembers.some((member) => member.email === userInfo?.email);
  const isTenantOwner = userInfo?.role === "Owner";
  const canViewMembers = isUserTeamMember || isTenantOwner;

  const currentUserMember = teamMembers.find((m) => m.email === userInfo?.email);
  const isCurrentUserMember = !!currentUserMember;
  const isCurrentUserAdmin = currentUserMember?.role === "Admin";
  const canEditMembers = (isCurrentUserAdmin || isTenantOwner) && isCurrentUserMember;

  const handleEditMembers = () => {
    setIsEditMembersDialogOpen(true);
  };

  const handleMembersUpdated = (updatedMembers: TeamMemberDetails[]) => {
    setTeamMembers(updatedMembers);
  };

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

  const canModifyTeam = userInfo?.role === "Owner";

  return (
    <>
      {isSmallScreen && <div className="fixed inset-0 z-[59] bg-black/50" aria-hidden="true" />}

      <section
        ref={sidePaneRef}
        className="relative z-[60] flex h-full w-full flex-col border-border border-l bg-background"
        aria-label={t`Team details`}
      >
        <XIcon
          ref={closeButtonRef}
          onClick={() => onClose()}
          className="absolute top-3 right-2 z-10 h-10 w-10 cursor-pointer p-2 hover:bg-muted focus:bg-muted focus:outline-none focus-visible:ring-2 focus-visible:ring-ring"
          aria-label={t`Close team details`}
          tabIndex={-1}
        />

        <div className="h-16 border-border border-t border-b bg-muted/30 px-4 py-8 backdrop-blur-sm">
          <Heading level={2} className="flex h-full items-center font-semibold text-base">
            <Trans>Team details</Trans>
          </Heading>
        </div>

        <div className="flex-1 overflow-y-auto">
          <div className="p-4">
            {isLoading && (
              <div className="flex items-center justify-center py-8">
                <Text className="text-muted-foreground text-sm">
                  <Trans>Loading...</Trans>
                </Text>
              </div>
            )}

            {error && (
              <div className="py-8 text-center">
                <Text className="text-destructive text-sm">
                  <Trans>Failed to load team details</Trans>
                </Text>
              </div>
            )}

            {teamDetails && (
              <TeamDetailsContent
                team={teamDetails}
                members={teamMembers}
                canViewMembers={canViewMembers}
                canEditMembers={canEditMembers}
                onEditMembers={handleEditMembers}
                isLoadingMembers={isLoadingMembers}
              />
            )}
          </div>
        </div>

        {canModifyTeam && teamDetails && (
          <div className="relative mt-auto space-y-2 border-border border-t bg-background p-4 pb-[max(1rem,env(safe-area-inset-bottom))]">
            <Button variant="outline" className="w-full justify-center text-sm" onPress={onEditTeam}>
              <EditIcon className="h-4 w-4" />
              <Trans>Edit Team</Trans>
            </Button>
            <Button variant="destructive" className="w-full justify-center text-sm" onPress={onDeleteTeam}>
              <Trash2Icon className="h-4 w-4" />
              <Trans>Delete Team</Trans>
            </Button>
          </div>
        )}
      </section>

      {teamDetails && (
        <EditTeamMembersDialog
          team={
            {
              id: teamDetails.id,
              name: teamDetails.name,
              description: teamDetails.description,
              memberCount: teamMembers.length
            } as TeamDetails
          }
          currentMembers={teamMembers}
          isOpen={isEditMembersDialogOpen}
          onOpenChange={setIsEditMembersDialogOpen}
          onMembersUpdated={handleMembersUpdated}
        />
      )}
    </>
  );
}
