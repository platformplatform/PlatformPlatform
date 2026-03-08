import type { useUsers } from "@repo/infrastructure/sync/hooks";

import { useCallback, useMemo } from "react";

type UserDetails = ReturnType<typeof useUsers>["data"][number];

interface UseUserSelectionProps {
  usersList: UserDetails[];
  selectedUsers: UserDetails[];
  onSelectedUsersChange: (users: UserDetails[]) => void;
  onViewProfile: (user: UserDetails | null, isKeyboardOpen?: boolean) => void;
}

export function useUserSelection({
  usersList,
  selectedUsers,
  onSelectedUsersChange,
  onViewProfile
}: UseUserSelectionProps) {
  const selectedUserIds = useMemo(() => new Set(selectedUsers.map((user) => user.id)), [selectedUsers]);

  const handleRowClick = useCallback(
    (user: UserDetails, event: React.MouseEvent) => {
      const target = event.target as HTMLElement;
      if (target.closest("button") || target.closest('[role="menuitem"]')) {
        return;
      }

      const clickedIndex = usersList.findIndex((u) => u.id === user.id);
      const isSelected = selectedUserIds.has(user.id);
      const isCtrlOrCmd = event.ctrlKey || event.metaKey;
      const isShift = event.shiftKey;

      if (isCtrlOrCmd) {
        if (isSelected) {
          const newSelection = selectedUsers.filter((u) => u.id !== user.id);
          onSelectedUsersChange(newSelection);
        } else {
          onSelectedUsersChange([...selectedUsers, user]);
        }
        onViewProfile(null);
      } else if (isShift && selectedUsers.length > 0) {
        const firstSelectedIndex = usersList.findIndex((u) => u.id === selectedUsers[0].id);
        const start = Math.min(firstSelectedIndex, clickedIndex);
        const end = Math.max(firstSelectedIndex, clickedIndex);
        const rangeUsers = usersList.slice(start, end + 1);
        onSelectedUsersChange(rangeUsers);
        onViewProfile(null);
      } else if (isSelected && selectedUsers.length === 1) {
        onSelectedUsersChange([]);
        onViewProfile(null);
      } else {
        onSelectedUsersChange([user]);
        onViewProfile(user, false);
      }
    },
    [usersList, selectedUserIds, selectedUsers, onSelectedUsersChange, onViewProfile]
  );

  const currentSelectedIndex =
    selectedUsers.length === 1 ? usersList.findIndex((u) => u.id === selectedUsers[0].id) : -1;

  return { selectedUserIds, handleRowClick, currentSelectedIndex };
}
