import { faker } from "@faker-js/faker";
import { isLocalhost } from "./constants";

/**
 * Generate a unique email with timestamp to ensure uniqueness
 * @param domain Optional domain, defaults to 'platformplatform.net'
 * @returns Unique email address
 */
export function uniqueEmail(domain = "platformplatform.net"): string {
  const timestamp = Date.now();
  const username = faker.internet.userName().toLowerCase();
  return `${username}.${timestamp}@${domain}`;
}

/**
 * Generate a random email using Faker
 * @returns Random email address
 */
export function randomEmail(): string {
  return faker.internet.email();
}

/**
 * Generate a random first name
 * @returns Random first name
 */
export function firstName(): string {
  return faker.person.firstName();
}

/**
 * Generate a random last name
 * @returns Random last name
 */
export function lastName(): string {
  return faker.person.lastName();
}

/**
 * Generate a random full name
 * @returns Random full name
 */
export function fullName(): string {
  return faker.person.fullName();
}

/**
 * Generate a random job title
 * @returns Random job title
 */
export function jobTitle(): string {
  return faker.person.jobTitle();
}

/**
 * Generate a random company name
 * @returns Random company name
 */
export function companyName(): string {
  return faker.company.name();
}

/**
 * Get the correct verification code for the current environment
 * @returns "UNLOCK" for local development, or instructions for CI
 */
export function getVerificationCode(): string {
  // For local development, always use "UNLOCK"
  if (isLocalhost()) {
    return "UNLOCK";
  }

  // For CI/CD environments, we'll need to implement email checking
  // For now, throw an error to remind us to implement this
  throw new Error("CI/CD verification code retrieval not yet implemented. Need to check email service.");
}

/**
 * Generate test user data
 * @returns Complete user data object
 */
export function testUser() {
  const first = firstName();
  const last = lastName();
  const timestamp = Date.now();
  const email = `${first.toLowerCase()}.${last.toLowerCase()}.${timestamp}@platformplatform.net`;

  return {
    email,
    firstName: first,
    lastName: last,
    fullName: `${first} ${last}`,
    jobTitle: jobTitle(),
    company: companyName()
  };
}
