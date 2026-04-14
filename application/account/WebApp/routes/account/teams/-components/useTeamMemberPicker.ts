import { t } from "@lingui/core/macro";
import { useUsers } from "@repo/infrastructure/sync/hooks";
import { type KeyboardEvent, type MouseEvent, useEffect, useMemo, useState } from "react";
import { toast } from "sonner";

import { api, queryClient } from "@/shared/lib/api/client";

import {
  buildPickerUsers,
  compareUsers,
  computePendingDiff,
  hasPendingChanges,
  matchesSearch,
  type PickerUser
} from "./teamMemberPickerUtils";

export type { PickerUser } from "./teamMemberPickerUtils";

interface UseTeamMemberPickerOptions {
  teamId: string | null;
  isOpen: boolean;
  onClose: () => void;
}

function toggleSelection(userId: string, selection: Set<string>, visibleIds: string[], additive: boolean): Set<string> {
  const next = new Set(additive ? selection : []);
  if (next.has(userId)) {
    next.delete(userId);
  } else {
    next.add(userId);
  }
  const visibleSet = new Set(visibleIds);
  for (const id of next) {
    if (!visibleSet.has(id)) {
      next.delete(id);
    }
  }
  return next;
}

export function useTeamMemberPicker({ teamId, isOpen, onClose }: UseTeamMemberPickerOptions) {
  const [search, setSearch] = useState("");
  const [selectedAvailable, setSelectedAvailable] = useState<Set<string>>(new Set());
  const [selectedMembers, setSelectedMembers] = useState<Set<string>>(new Set());
  const [pendingMemberIds, setPendingMemberIds] = useState<Set<string>>(new Set());

  const { data: allUsers } = useUsers();

  const membersQuery = api.useQuery(
    "get",
    "/api/account/teams/{teamId}/members",
    { params: { path: { teamId: teamId ?? "" } } },
    { enabled: isOpen && teamId !== null }
  );

  const updateMembersMutation = api.useMutation("put", "/api/account/teams/{teamId}/members", {
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["get", "/api/account/teams/{teamId}/members"] });
      queryClient.invalidateQueries({ queryKey: ["get", "/api/account/teams"] });
      toast.success(t`Team members updated successfully`);
      onClose();
    }
  });

  const originalMembers = membersQuery.data?.members;

  useEffect(() => {
    if (isOpen && originalMembers) {
      setPendingMemberIds(new Set(originalMembers.map((member) => member.userId)));
      setSelectedAvailable(new Set());
      setSelectedMembers(new Set());
      setSearch("");
    }
  }, [isOpen, originalMembers]);

  useEffect(() => {
    if (!isOpen) {
      setSearch("");
      setSelectedAvailable(new Set());
      setSelectedMembers(new Set());
    }
  }, [isOpen]);

  const allPickerUsers = useMemo(() => buildPickerUsers(allUsers, originalMembers), [allUsers, originalMembers]);

  const normalizedSearch = search.trim().toLowerCase();

  const filterAndSort = (predicate: (user: PickerUser) => boolean) =>
    allPickerUsers
      .filter(predicate)
      .filter((user) => matchesSearch(user, normalizedSearch))
      .sort(compareUsers);

  const availableUsers = useMemo(
    () => filterAndSort((user) => !pendingMemberIds.has(user.userId)),
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [allPickerUsers, pendingMemberIds, normalizedSearch]
  );

  const teamMembers = useMemo(
    () => filterAndSort((user) => pendingMemberIds.has(user.userId)),
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [allPickerUsers, pendingMemberIds, normalizedSearch]
  );

  const addSelected = () => {
    if (selectedAvailable.size === 0) {
      return;
    }
    setPendingMemberIds((previous) => new Set([...previous, ...selectedAvailable]));
    setSelectedAvailable(new Set());
  };

  const removeSelected = () => {
    if (selectedMembers.size === 0) {
      return;
    }
    setPendingMemberIds((previous) => {
      const next = new Set(previous);
      for (const id of selectedMembers) {
        next.delete(id);
      }
      return next;
    });
    setSelectedMembers(new Set());
  };

  const addUser = (userId: string) => {
    setPendingMemberIds((previous) => new Set([...previous, userId]));
    setSelectedAvailable(new Set());
  };

  const removeUser = (userId: string) => {
    setPendingMemberIds((previous) => {
      const next = new Set(previous);
      next.delete(userId);
      return next;
    });
    setSelectedMembers(new Set());
  };

  const onAvailableActivate = (event: MouseEvent | KeyboardEvent, userId: string) => {
    setSelectedAvailable(
      toggleSelection(
        userId,
        selectedAvailable,
        availableUsers.map((user) => user.userId),
        event.ctrlKey || event.metaKey
      )
    );
  };

  const onMemberActivate = (event: MouseEvent | KeyboardEvent, userId: string) => {
    setSelectedMembers(
      toggleSelection(
        userId,
        selectedMembers,
        teamMembers.map((user) => user.userId),
        event.ctrlKey || event.metaKey
      )
    );
  };

  const save = () => {
    if (!teamId || !originalMembers) {
      return;
    }
    const body = computePendingDiff(originalMembers, pendingMemberIds);
    updateMembersMutation.mutate({ params: { path: { teamId } }, body });
  };

  return {
    search,
    setSearch,
    isLoading: membersQuery.isLoading || allUsers.length === 0,
    availableUsers,
    teamMembers,
    selectedAvailable,
    selectedMembers,
    onAvailableActivate,
    onMemberActivate,
    addSelected,
    removeSelected,
    addUser,
    removeUser,
    save,
    hasPendingChanges: hasPendingChanges(originalMembers, pendingMemberIds),
    isSaving: updateMembersMutation.isPending
  };
}
