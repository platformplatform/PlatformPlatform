import { electricCollectionOptions } from "@tanstack/electric-db-collection";
import { createCollection } from "@tanstack/react-db";
import { createShapeOptions } from "./electricConfig";
import type { SessionRow, SubscriptionRow, TenantRow, UserRow } from "./types";

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

export const sessionCollection = createCollection<SessionRow>(
  electricCollectionOptions({
    id: "sessions",
    shapeOptions: createShapeOptions("sessions"),
    getKey: (item) => item.id,
    syncMode: "on-demand"
  })
);
