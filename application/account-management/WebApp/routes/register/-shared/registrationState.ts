interface Registration {
  accountRegistrationId: string;
  email: string;
  expireAt: Date;
}

let currentRegistration: Registration | undefined;

export function setRegistration(newRegistration: Registration): void {
  currentRegistration = newRegistration;
}

export function getRegistration() {
  if (currentRegistration == null) throw new Error("Account registration ID is missing.");
  return currentRegistration;
}
