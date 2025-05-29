import type { Schemas } from "@/shared/lib/api/client";

interface SignupState {
  emailConfirmationId: Schemas["EmailConfirmationId"];
  email: string;
  expireAt: Date;
}

let currentSignupState: SignupState | undefined;

export function setSignupState(newSignup: SignupState): void {
  currentSignupState = newSignup;
}

export function clearSignupState(): void {
  currentSignupState = undefined;
}

export function hasSignupState(): boolean {
  return currentSignupState != null;
}

export function getSignupState() {
  return currentSignupState as SignupState;
}
