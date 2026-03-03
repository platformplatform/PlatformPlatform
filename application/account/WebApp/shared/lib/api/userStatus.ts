import { t } from "@lingui/core/macro";

export enum UserStatus {
  Active = "Active",
  Pending = "Pending"
}

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
