/**
 * Shared state for auth synchronization
 * Used to coordinate between auth sync detection and API calls
 */

let hasPendingAuthSync = false;

export function setHasPendingAuthSync(value: boolean) {
  hasPendingAuthSync = value;
}

export function getHasPendingAuthSync(): boolean {
  return hasPendingAuthSync;
}