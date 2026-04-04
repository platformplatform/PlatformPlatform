import type { ChangeMessage } from "@tanstack/react-db";

import { electricCollectionOptions } from "@tanstack/electric-db-collection";
import { createCollection } from "@tanstack/react-db";

import type { FeatureFlagRow, SubscriptionRow, TenantRow, UserRow } from "./types";

import { getLastElectricOffset } from "../http/queryClient";
import { createShapeOptions } from "./electricConfig";
import { markFeatureFlagChanged } from "./featureFlagChangeTracker";

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

featureFlagCollection.subscribeChanges((changes: Array<ChangeMessage<FeatureFlagRow>>) => {
  for (const change of changes) {
    if (change.type === "insert" || change.type === "delete") {
      markFeatureFlagChanged();
      return;
    }
    if (change.type === "update" && change.previousValue) {
      if (
        change.value.enabledAt !== change.previousValue.enabledAt ||
        change.value.disabledAt !== change.previousValue.disabledAt
      ) {
        markFeatureFlagChanged();
        return;
      }
    }
  }
});

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
