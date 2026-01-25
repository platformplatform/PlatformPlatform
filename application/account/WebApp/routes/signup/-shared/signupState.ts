import type { Schemas } from "@/shared/lib/api/client";

interface SignupState {
  emailLoginId: Schemas["EmailLoginId"];
  email: string;
  expireAt: Date;
  codeCount: number;
  hasRequestedNewCode: boolean;
  autoSubmitCode: boolean;
  lastSubmittedCode?: string;
  currentOtpValue?: string;
  validForSeconds?: number;
}

let currentSignupState: SignupState | undefined;

export function setSignupState(newSignup: Partial<SignupState>): void {
  if (!currentSignupState) {
    currentSignupState = {
      ...(newSignup as SignupState),
      codeCount: 1, // First code
      hasRequestedNewCode: false,
      autoSubmitCode: true, // Default to auto-submit
      lastSubmittedCode: "", // Initialize with empty string
      currentOtpValue: "" // Initialize with empty string
    };
  } else {
    currentSignupState = {
      ...currentSignupState,
      ...newSignup
    };
  }
}

export function incrementCodeCount(): void {
  if (currentSignupState) {
    currentSignupState.codeCount += 1;
  }
}

export function setHasRequestedNewCode(value: boolean): void {
  if (currentSignupState) {
    currentSignupState.hasRequestedNewCode = value;
  }
}

export function setAutoSubmitCode(value: boolean): void {
  if (currentSignupState) {
    currentSignupState.autoSubmitCode = value;
  }
}

export function setLastSubmittedCode(code: string): void {
  if (currentSignupState) {
    currentSignupState.lastSubmittedCode = code;
  }
}

export function clearSignupState(): void {
  currentSignupState = undefined;
}

export function hasSignupState(): boolean {
  return currentSignupState != null;
}

export function getSignupState(): Partial<SignupState> {
  return currentSignupState || { email: "", codeCount: 0, hasRequestedNewCode: false, autoSubmitCode: true };
}
