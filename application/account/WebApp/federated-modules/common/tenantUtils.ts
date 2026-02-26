import { enhancedFetch } from "@repo/infrastructure/http/httpClient";

export interface TenantInfo {
  tenantId: string;
  tenantName: string | null;
  logoUrl: string | null;
  isNew: boolean;
}

export interface TenantsResponse {
  tenants: TenantInfo[];
}

export function sortTenants(tenants: TenantInfo[]): TenantInfo[] {
  return [...tenants].sort((a, b) => {
    if (!a.tenantName && b.tenantName) {
      return 1;
    }
    if (a.tenantName && !b.tenantName) {
      return -1;
    }
    const nameA = a.tenantName || "";
    const nameB = b.tenantName || "";
    return nameA.localeCompare(nameB);
  });
}

export async function fetchTenants(): Promise<TenantsResponse> {
  const response = await enhancedFetch("/api/account/tenants");
  return response.json();
}

export async function switchTenantApi(tenantId: string): Promise<void> {
  await enhancedFetch("/api/account/authentication/switch-tenant", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ tenantId })
  });
}

export async function logoutApi(): Promise<void> {
  await enhancedFetch("/api/account/authentication/logout", {
    method: "POST"
  });
}
