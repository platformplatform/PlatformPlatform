/**
 * Get initials from a user's name or email.
 * If first and last name are provided, returns their first letters.
 * If only email is provided, returns first two letters of the email username.
 */
export function getInitials(
  firstName: string | undefined,
  lastName: string | undefined,
  email: string | undefined
): string {
  if (firstName && lastName) {
    return `${firstName[0]}${lastName[0]}`;
  }
  if (email == null) {
    return "";
  }
  return email.split("@")[0].slice(0, 2).toUpperCase();
}
