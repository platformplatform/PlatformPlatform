import type { Schemas } from "@/shared/lib/api/client";
import { t } from "@lingui/core/macro";

interface LoginState {
  loginId: Schemas["LoginId"];
  emailConfirmationId: Schemas["EmailConfirmationId"];
  email: string;
  expireAt: Date;
}

let currentLoginState: LoginState | undefined;

export function setLoginState(newLogin: LoginState): void {
  currentLoginState = newLogin;
}

export function getLoginState() {
  if (currentLoginState == null) {
    throw new Error(t`No active login.`);
  }
  return currentLoginState;
}
