import { electricCollectionOptions } from "@tanstack/electric-db-collection";
import { createCollection } from "@tanstack/react-db";

import type { SubscriptionRow, TenantRow, UserRow } from "./types";

import { createShapeOptions } from "./electricConfig";

export const userCollection = createCollection<UserRow>(
  electricCollectionOptions({
    id: "users",
    shapeOptions: createShapeOptions("users"),
    getKey: (item) => item.id,
    syncMode: "on-demand"
  })
);

export const tenantCollection = createCollection<TenantRow>(
  electricCollectionOptions({
    id: "tenants",
    shapeOptions: createShapeOptions("tenants"),
    getKey: (item) => item.id,
    syncMode: "eager"
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
