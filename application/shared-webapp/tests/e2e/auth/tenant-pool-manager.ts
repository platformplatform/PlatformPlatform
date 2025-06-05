/**
 * Set to track which tenants have been used for rate-limiting tests
 */
const rateLimitingUsedTenants = new Set<string>();

/**
 * Register a tenant as used for rate-limiting tests
 * @param tenantId Tenant ID to mark as rate-limiting used
 */
export function markTenantAsRateLimitingUsed(tenantId: string): void {
  rateLimitingUsedTenants.add(tenantId);
}

/**
 * Check if a tenant has been used for rate-limiting tests
 * @param tenantId Tenant ID to check
 * @returns True if tenant has been used for rate-limiting
 */
export function isTenantRateLimitingUsed(tenantId: string): boolean {
  return rateLimitingUsedTenants.has(tenantId);
}
