import type { Schemas } from "@/shared/lib/api/client";

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

export function clearLoginState(): void {
  currentLoginState = undefined;
}

export function hasLoginState(): boolean {
  return currentLoginState != null;
}

export function getLoginState() {
  return currentLoginState as LoginState;
}
