/**
 * Error thrown when the verification code has expired
 */
export class VerificationExpirationError extends Error {
  constructor(public accountRegistrationId: string) {
    super("Verification code expired");
  }
}
