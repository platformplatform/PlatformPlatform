import { t } from "@lingui/core/macro";
import { UserRole } from "@/shared/lib/api/client";

export function getUserRoleLabel(role: UserRole): string {
  switch (role) {
    case UserRole.Member:
      return t`Member`;
    case UserRole.Admin:
      return t`Admin`;
    case UserRole.Owner:
      return t`Owner`;
    default: {
      return String(role);
    }
  }
}
