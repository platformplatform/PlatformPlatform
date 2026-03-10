import { electricCollectionOptions } from "@tanstack/electric-db-collection";
import { createCollection } from "@tanstack/react-db";

import type { FeatureFlagRow, SubscriptionRow, TenantRow, UserRow } from "./types";

import { getLastElectricOffset } from "../http/queryClient";
import { createShapeOptions } from "./electricConfig";

function txidHandler() {
  const offset = getLastElectricOffset();
  if (offset != null) {
    return { txid: offset };
  }
}

export const userCollection = createCollection<UserRow>(
  electricCollectionOptions({
    id: "users",
    shapeOptions: createShapeOptions("users"),
    getKey: (item) => item.id,
    syncMode: "on-demand",
    onInsert: async () => txidHandler(),
    onUpdate: async () => txidHandler(),
    onDelete: async () => txidHandler()
  })
);

export const tenantCollection = createCollection<TenantRow>(
  electricCollectionOptions({
    id: "tenants",
    shapeOptions: createShapeOptions("tenants"),
    getKey: (item) => item.id,
    syncMode: "eager",
    onUpdate: async () => txidHandler()
  })
);

export const subscriptionCollection = createCollection<SubscriptionRow>(
  electricCollectionOptions({
    id: "subscriptions",
    shapeOptions: createShapeOptions("subscriptions"),
    getKey: (item) => item.id,
    syncMode: "eager"
  })
);

export const featureFlagCollection = createCollection<FeatureFlagRow>(
  electricCollectionOptions({
    id: "feature_flags",
    shapeOptions: createShapeOptions("feature_flags"),
    getKey: (item) => item.id,
    syncMode: "eager",
    onInsert: async () => txidHandler(),
    onUpdate: async () => txidHandler(),
    onDelete: async () => txidHandler()
  })
);

declare global {
  interface Window {
    __electricCollections?: {
      userCollection: typeof userCollection;
      tenantCollection: typeof tenantCollection;
      subscriptionCollection: typeof subscriptionCollection;
      featureFlagCollection: typeof featureFlagCollection;
    };
  }
}

window.__electricCollections = { userCollection, tenantCollection, subscriptionCollection, featureFlagCollection };
