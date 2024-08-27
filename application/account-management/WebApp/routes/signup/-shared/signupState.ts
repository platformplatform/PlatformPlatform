import type { Schemas } from "@/shared/lib/api/client";

interface SignupState {
  signupId: Schemas["SignupId"];
  email: string;
  expireAt: Date;
}

let currentSignupState: SignupState | undefined;

export function setSignupState(newSignup: SignupState): void {
  currentSignupState = newSignup;
}

export function getSignupState() {
  if (currentSignupState == null) throw new Error("No active signup.");
  return currentSignupState;
}
