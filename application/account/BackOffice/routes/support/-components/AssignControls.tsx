import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Avatar, AvatarFallback } from "@repo/ui/components/Avatar";
import { Button } from "@repo/ui/components/Button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger
} from "@repo/ui/components/DropdownMenu";
import { Tooltip, TooltipContent, TooltipTrigger } from "@repo/ui/components/Tooltip";
import { useQueryClient } from "@tanstack/react-query";
import { CheckIcon, ChevronDownIcon, UserMinusIcon, UserPlusIcon } from "lucide-react";
import { useMemo } from "react";
import { toast } from "sonner";

import { useMe } from "@/shared/hooks/useMe";
import { api, type Schemas } from "@/shared/lib/api/client";

import { getInitials } from "./displayName";

type Assignee = Schemas["StaffTicketAssignee"];

interface AssignControlsProps {
  ticketId: string;
  currentAssignee: Assignee | null | undefined;
}

interface Teammate {
  objectId: string;
  displayName: string;
  isCurrentUser: boolean;
}

export function AssignControls({ ticketId, currentAssignee }: Readonly<AssignControlsProps>) {
  const { data: me } = useMe();
  const meObjectId = me?.objectId;
  const meDisplayName = me?.displayName ?? "";
  const queryClient = useQueryClient();

  // Derive the teammate list from the union of assignees across the unfiltered ticket page + the
  // current staff user, deduped by objectId. Always reads the unfiltered cache entry to maximize the
  // teammate pool — that's a separate cache entry from a filtered inbox view, so this fires its own
  // request whenever the inbox is filtered. Acceptable cost for the simpler derivation.
  // TODO: full back-office staff directory pending dedicated endpoint
  const { data: ticketsData } = api.useQuery("get", "/api/back-office/support-tickets", undefined, {
    staleTime: 60_000
  });

  const teammates = useMemo<Teammate[]>(() => {
    const map = new Map<string, Teammate>();
    if (meObjectId) {
      map.set(meObjectId, { objectId: meObjectId, displayName: meDisplayName, isCurrentUser: true });
    }
    if (currentAssignee && !map.has(currentAssignee.objectId)) {
      map.set(currentAssignee.objectId, {
        objectId: currentAssignee.objectId,
        displayName: currentAssignee.displayName,
        isCurrentUser: currentAssignee.objectId === meObjectId
      });
    }
    for (const ticket of ticketsData?.tickets ?? []) {
      if (ticket.assignee && !map.has(ticket.assignee.objectId)) {
        map.set(ticket.assignee.objectId, {
          objectId: ticket.assignee.objectId,
          displayName: ticket.assignee.displayName,
          isCurrentUser: ticket.assignee.objectId === meObjectId
        });
      }
    }
    return Array.from(map.values()).sort((a, b) => a.displayName.localeCompare(b.displayName));
  }, [meObjectId, meDisplayName, currentAssignee, ticketsData?.tickets]);

  const invalidateTickets = () => {
    queryClient.invalidateQueries({ queryKey: ["get", "/api/back-office/support-tickets"] });
    queryClient.invalidateQueries({ queryKey: ["get", "/api/back-office/support-tickets/{id}"] });
  };

  const assignMutation = api.useMutation("put", "/api/back-office/support-tickets/{id}/assignee", {
    onSuccess: () => {
      toast.success(t`Assignee updated`);
      invalidateTickets();
    }
  });

  const assignTo = (teammate: Teammate) => {
    assignMutation.mutate({
      params: { path: { id: ticketId } },
      body: { assigneeObjectId: teammate.objectId, assigneeDisplayName: teammate.displayName }
    });
  };

  const unassign = () => {
    assignMutation.mutate({
      params: { path: { id: ticketId } },
      body: { assigneeObjectId: null, assigneeDisplayName: null }
    });
  };

  const isAssignedToMe = currentAssignee?.objectId === meObjectId;
  const isPending = assignMutation.isPending;
  const meReady = !!meObjectId;

  const primary = (
    <Button
      variant="outline"
      size="sm"
      className="flex-1 rounded-r-none"
      onClick={() => meObjectId && assignTo({ objectId: meObjectId, displayName: meDisplayName, isCurrentUser: true })}
      isPending={isPending}
      disabled={!meReady || isAssignedToMe}
    >
      <UserPlusIcon className="size-3.5" />
      {isAssignedToMe ? <Trans>Assigned to you</Trans> : <Trans>Assign to me</Trans>}
    </Button>
  );

  return (
    <div className="flex w-full items-stretch">
      {meReady ? (
        primary
      ) : (
        <Tooltip>
          <TooltipTrigger render={primary} />
          <TooltipContent>
            <Trans>Loading…</Trans>
          </TooltipContent>
        </Tooltip>
      )}
      <DropdownMenu trackingTitle="Assign controls">
        <Tooltip>
          <TooltipTrigger
            render={
              <DropdownMenuTrigger
                render={
                  <Button
                    variant="outline"
                    size="sm"
                    className="-ml-px rounded-l-none px-2"
                    aria-label={t`More assignment options`}
                    disabled={isPending}
                  >
                    <ChevronDownIcon className="size-3.5" />
                  </Button>
                }
              />
            }
          />
          <TooltipContent>{t`More assignment options`}</TooltipContent>
        </Tooltip>
        <DropdownMenuContent align="end" className="min-w-[14rem]">
          <DropdownMenuItem onClick={unassign} disabled={!currentAssignee} trackingLabel="Unassign">
            <UserMinusIcon className="size-4" />
            <Trans>Unassign</Trans>
          </DropdownMenuItem>
          {teammates.length > 0 && <DropdownMenuSeparator />}
          {teammates.map((teammate) => {
            const isActive = currentAssignee?.objectId === teammate.objectId;
            return (
              <TeammateMenuItem
                key={teammate.objectId}
                teammate={teammate}
                isActive={isActive}
                onClick={() => assignTo(teammate)}
              />
            );
          })}
        </DropdownMenuContent>
      </DropdownMenu>
    </div>
  );
}

function TeammateMenuItem({
  teammate,
  isActive,
  onClick
}: {
  teammate: Teammate;
  isActive: boolean;
  onClick: () => void;
}) {
  return (
    <DropdownMenuItem
      onClick={onClick}
      trackingLabel={teammate.isCurrentUser ? "Assign to me" : "Assign to teammate"}
      disabled={isActive}
    >
      <Avatar size="default" className="size-6">
        <AvatarFallback className="bg-primary/10 text-[0.625rem] text-primary">
          {getInitials(teammate.displayName)}
        </AvatarFallback>
      </Avatar>
      <span className="flex-1 truncate">
        {teammate.displayName}
        {teammate.isCurrentUser && (
          <span className="ml-1 text-muted-foreground">
            <Trans>(you)</Trans>
          </span>
        )}
      </span>
      {isActive && <CheckIcon className="size-3.5" />}
    </DropdownMenuItem>
  );
}
