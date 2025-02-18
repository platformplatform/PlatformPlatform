# Changelog

## [Unreleased]

### Added
- Added `EmailConfirmationType` to `EmailConfirmation` domain model to distinguish between different confirmation scenarios (Signup, Login, ChangeEmail, DeleteTenant)
- Added telemetry events for email confirmation workflows:
  - `EmailConfirmationStarted`
  - `EmailConfirmationCompleted`
  - `EmailConfirmationFailed`
  - `EmailConfirmationBlocked`
  - `EmailConfirmationExpired`
- Updated `StartEmailConfirmation` and `CompleteEmailConfirmation` to use `IEmailConfirmationRepository` for better abstraction

### Changed
- Removed `Signup` aggregate and related files, now using `EmailConfirmation` directly for the signup process
- Updated `StartSignup` and `CompleteSignup` commands to work directly with `EmailConfirmation`
- Simplified database schema by removing the `Signups` table
- Refactored login process to use `EmailConfirmation`:
  - Updated `Login` domain model to store `EmailConfirmationId` instead of verification code properties
  - Modified `StartLogin` and `CompleteLogin` to use `EmailConfirmation` for email verification
  - Updated database schema to reflect changes in `Login` table
  - Standardized email templates across signup and login flows

All notable changes to this project will be documented in this file.
