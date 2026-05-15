import { t } from "@lingui/core/macro";
import { ToggleGroup, ToggleGroupItem } from "@repo/ui/components/ToggleGroup";
import { useNavigate } from "@tanstack/react-router";

import { UserRole } from "@/shared/lib/api/client";
import { getUserRoleLabel } from "@/shared/lib/api/labels";

import type { StateFilter } from "./stateFilter";

import { FeatureFlagAudienceToolbar } from "./FeatureFlagAudienceToolbar";

interface FeatureFlagUsersToolbarProps {
  flagKey: string;
  search: string | undefined;
  roles: UserRole[];
  state: StateFilter | undefined;
  hasOverride: boolean;
  hideHasOverride?: boolean;
  hideState?: boolean;
}

export function FeatureFlagUsersToolbar({
  flagKey,
  search,
  roles,
  state,
  hasOverride,
  hideHasOverride,
  hideState
}: Readonly<FeatureFlagUsersToolbarProps>) {
  const navigate = useNavigate();

  const handleRolesChange = (values: string[]) => {
    const next = values as UserRole[];
    navigate({
      to: "/feature-flags/$flagKey",
      params: { flagKey },
      search: (previous) => ({
        ...previous,
        usersRoles: next.length === 0 ? undefined : next,
        usersPageOffset: undefined
      })
    });
  };

  return (
    <FeatureFlagAudienceToolbar
      search={search}
      searchPlaceholder={t`Search by name or email`}
      state={state}
      hasOverride={hasOverride}
      hideHasOverride={hideHasOverride}
      hideState={hideState}
      onSearchChange={(next) =>
        navigate({
          to: "/feature-flags/$flagKey",
          params: { flagKey },
          search: (previous) => ({ ...previous, usersSearch: next, usersPageOffset: undefined })
        })
      }
      onStateChange={(next) =>
        navigate({
          to: "/feature-flags/$flagKey",
          params: { flagKey },
          search: (previous) => ({ ...previous, usersState: next, usersPageOffset: undefined })
        })
      }
      onHasOverrideChange={(next) =>
        navigate({
          to: "/feature-flags/$flagKey",
          params: { flagKey },
          search: (previous) => ({
            ...previous,
            usersHasOverride: next ? true : undefined,
            usersPageOffset: undefined
          })
        })
      }
    >
      <ToggleGroup
        variant="outline"
        aria-label={t`Role`}
        multiple={true}
        value={roles}
        onValueChange={handleRolesChange}
      >
        {[UserRole.Owner, UserRole.Admin, UserRole.Member].map((value) => (
          <ToggleGroupItem key={value} value={value} className="min-w-[5rem] justify-center">
            {getUserRoleLabel(value)}
          </ToggleGroupItem>
        ))}
      </ToggleGroup>
    </FeatureFlagAudienceToolbar>
  );
}
