import type { Schemas } from "@/shared/lib/api/client";

interface RegistrationState {
  accountRegistrationId: Schemas["AccountRegistrationId"];
  email: string;
  expireAt: Date;
}

let currentRegistrationState: RegistrationState | undefined;

export function setRegistrationState(newRegistration: RegistrationState): void {
  currentRegistrationState = newRegistration;
}

export function getRegistrationState() {
  if (currentRegistrationState == null) throw new Error("No active account registration.");
  return currentRegistrationState;
}
