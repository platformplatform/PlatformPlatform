import { useEffect, useRef } from "react";
import { type AuthSyncMessage, authSyncService } from "../auth/AuthSyncService";
import { useIsAuthenticated } from "../auth/hooks";
import { sessionCollection, subscriptionCollection, tenantCollection, userCollection } from "./collections";

const allCollections = [userCollection, tenantCollection, subscriptionCollection, sessionCollection];

function cleanupCollections() {
  for (const collection of allCollections) {
    collection.cleanup();
  }
}

export function SyncProvider({ children }: { children: React.ReactNode }) {
  const isAuthenticated = useIsAuthenticated();
  const previouslyAuthenticated = useRef(false);

  useEffect(() => {
    if (isAuthenticated && !previouslyAuthenticated.current) {
      previouslyAuthenticated.current = true;
    }

    if (!isAuthenticated && previouslyAuthenticated.current) {
      previouslyAuthenticated.current = false;
      cleanupCollections();
    }
  }, [isAuthenticated]);

  useEffect(() => {
    const unsubscribe = authSyncService.subscribe((message: AuthSyncMessage) => {
      if (message.type === "TENANT_SWITCHED" || message.type === "USER_LOGGED_OUT") {
        cleanupCollections();
      }
    });

    return unsubscribe;
  }, []);

  return <>{children}</>;
}
