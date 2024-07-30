interface VerificationInfo {
  id: string;
  email: string;
  expireAt: Date;
}

let currentVerificationInfo: VerificationInfo | undefined;

export function setVerificationInfo(newVerificationInfo: VerificationInfo): void {
  currentVerificationInfo = newVerificationInfo;
}

export function getVerificationInfo() {
  if (currentVerificationInfo == null) throw new Error("Verification info is missing.");
  return currentVerificationInfo;
}
