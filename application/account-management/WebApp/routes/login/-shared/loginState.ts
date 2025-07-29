import type { Schemas } from "@/shared/lib/api/client";

interface LoginState {
  loginId: Schemas["LoginId"];
  emailConfirmationId: Schemas["EmailConfirmationId"];
  email: string;
  expireAt: Date;
  codeCount: number;
  hasRequestedNewCode: boolean;
  autoSubmitCode: boolean;
  lastSubmittedCode?: string;
  currentOtpValue?: string;
  validForSeconds?: number;
}

let currentLoginState: LoginState | undefined;

export function setLoginState(newLogin: Partial<LoginState>): void {
  if (!currentLoginState) {
    currentLoginState = {
      ...(newLogin as LoginState),
      codeCount: 1, // First code
      hasRequestedNewCode: false,
      autoSubmitCode: true, // Default to auto-submit
      lastSubmittedCode: "", // Initialize with empty string
      currentOtpValue: "" // Initialize with empty string
    };
  } else {
    currentLoginState = {
      ...currentLoginState,
      ...newLogin
    };
  }
}

export function incrementCodeCount(): void {
  if (currentLoginState) {
    currentLoginState.codeCount += 1;
  }
}

export function setHasRequestedNewCode(value: boolean): void {
  if (currentLoginState) {
    currentLoginState.hasRequestedNewCode = value;
  }
}

export function setAutoSubmitCode(value: boolean): void {
  if (currentLoginState) {
    currentLoginState.autoSubmitCode = value;
  }
}

export function setLastSubmittedCode(code: string): void {
  if (currentLoginState) {
    currentLoginState.lastSubmittedCode = code;
  }
}

export function clearLoginState(): void {
  currentLoginState = undefined;
}

export function hasLoginState(): boolean {
  return currentLoginState != null;
}

export function getLoginState(): Partial<LoginState> {
  return currentLoginState || { email: "", codeCount: 0, hasRequestedNewCode: false, autoSubmitCode: true };
}
