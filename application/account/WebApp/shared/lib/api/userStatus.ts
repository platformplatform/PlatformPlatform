import { t } from "@lingui/core/macro";
import { UserStatus } from "@/shared/lib/api/client";

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
