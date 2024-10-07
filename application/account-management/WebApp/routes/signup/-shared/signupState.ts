import type { Schemas } from "@/shared/lib/api/client";
import { t } from "@lingui/macro";

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
  if (currentSignupState == null) throw new Error(t`No active signup session`);
  return currentSignupState;
}
