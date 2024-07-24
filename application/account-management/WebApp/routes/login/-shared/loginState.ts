import type { Schemas } from "@/shared/lib/api/client";

interface LoginState {
  loginId: Schemas["LoginId"];
  email: string;
  expireAt: Date;
}

let currentLoginState: LoginState | undefined;

export function setLoginState(newLogin: LoginState): void {
  currentLoginState = newLogin;
}

export function getLoginState() {
  if (currentLoginState == null) throw new Error("No active login.");
  return currentLoginState;
}
