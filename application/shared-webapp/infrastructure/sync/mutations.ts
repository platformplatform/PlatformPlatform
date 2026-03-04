import { enhancedFetch } from "../http/httpClient";
import { subscriptionCollection, tenantCollection, userCollection } from "./collections";
import { getElectricOffset } from "./electricConfig";

async function mutateAndAwaitSync(url: string, method: string, body?: unknown): Promise<number | undefined> {
  const init: RequestInit = { method };
  if (body !== undefined) {
    init.headers = { "Content-Type": "application/json" };
    init.body = JSON.stringify(body);
  }
  const response = await enhancedFetch(url, init);
  return getElectricOffset(response);
}

export async function updateUser(
  userId: string,
  data: { firstName: string; lastName: string; title: string }
): Promise<void> {
  const txid = await mutateAndAwaitSync(`/api/account/users/${userId}`, "PUT", data);
  if (txid != null) {
    await userCollection.utils.awaitTxId(txid);
  }
}

export async function updateCurrentUser(data: { firstName: string; lastName: string; title: string }): Promise<void> {
  const txid = await mutateAndAwaitSync("/api/account/users/me", "PUT", data);
  if (txid != null) {
    await userCollection.utils.awaitTxId(txid);
  }
}

export async function updateTenant(data: { name: string }): Promise<void> {
  const txid = await mutateAndAwaitSync("/api/account/tenants/current", "PUT", data);
  if (txid != null) {
    await tenantCollection.utils.awaitTxId(txid);
  }
}

export async function awaitSubscriptionSync(txid: number): Promise<void> {
  await subscriptionCollection.utils.awaitTxId(txid);
}
