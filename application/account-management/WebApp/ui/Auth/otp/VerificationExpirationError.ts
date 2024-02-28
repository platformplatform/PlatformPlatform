/**
 * Error thrown when the verification code has expired
 */
export class VerificationExpirationError extends Error {
  constructor() {
    super("Verification code expired");
  }
}
