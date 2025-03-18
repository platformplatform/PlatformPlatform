import type { Schemas } from "@/shared/lib/api/client";
import { t } from "@lingui/core/macro";

interface SignupState {
  emailConfirmationId: Schemas["EmailConfirmationId"];
  email: string;
  expireAt: Date;
}

let currentSignupState: SignupState | undefined;

export function setSignupState(newSignup: SignupState): void {
  currentSignupState = newSignup;
}

export function getSignupState() {
  if (currentSignupState == null) {
    throw new Error(t`No active signup session.`);
  }
  return currentSignupState;
}
