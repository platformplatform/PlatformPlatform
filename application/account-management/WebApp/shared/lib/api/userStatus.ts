import { t } from "@lingui/core/macro";

export type UserStatus = "Active" | "Pending";

export const USER_STATUSES: UserStatus[] = ["Active", "Pending"];

export function getStatusLabel(status: UserStatus): string {
  switch (status) {
    case "Active":
      return t`Active`;
    case "Pending":
      return t`Pending`;
    default: {
      return String(status);
    }
  }
}
