import { UserStatus } from "@/shared/lib/api/client";
import { t } from "@lingui/core/macro";

export function getUserStatusLabel(userStatus: UserStatus): string {
  switch (userStatus) {
    case UserStatus.Active:
      return t`Active`;
    case UserStatus.Pending:
      return t`Pending`;
    default: {
      return String(userStatus);
    }
  }
}
