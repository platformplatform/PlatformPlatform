import { t } from "@lingui/core/macro";
import { UserRole } from "@/shared/lib/api/client";

export function getUserRoleLabel(userRole: UserRole): string {
  switch (userRole) {
    case UserRole.Member:
      return t`Member`;
    case UserRole.Admin:
      return t`Admin`;
    case UserRole.Owner:
      return t`Owner`;
    default: {
      return String(userRole);
    }
  }
}
