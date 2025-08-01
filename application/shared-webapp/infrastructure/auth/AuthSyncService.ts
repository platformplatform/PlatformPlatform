/**
 * Cross-tab authentication synchronization service
 *
 * Handles communication between browser tabs to synchronize authentication state:
 * - Tenant switching
 * - Login events
 * - Logout events
 *
 * Uses BroadcastChannel API for real-time cross-tab messaging
 */

export type AuthSyncEventType = "TENANT_SWITCHED" | "USER_LOGGED_IN" | "USER_LOGGED_OUT";

export interface TenantSwitchedMessage {
  type: "TENANT_SWITCHED";
  newTenantId: string;
  previousTenantId: string;
  tenantName: string;
  userId: string;
  timestamp: number;
}

export interface UserLoggedInMessage {
  type: "USER_LOGGED_IN";
  userId: string;
  tenantId: string;
  email: string;
  timestamp: number;
}

export interface UserLoggedOutMessage {
  type: "USER_LOGGED_OUT";
  userId: string;
  timestamp: number;
}

export type AuthSyncMessage = TenantSwitchedMessage | UserLoggedInMessage | UserLoggedOutMessage;

type AuthSyncListener = (message: AuthSyncMessage) => void;

class AuthSyncService {
  private channel: BroadcastChannel | null = null;
  private listeners: Set<AuthSyncListener> = new Set();
  private lastProcessedTimestamp = 0;

  constructor() {
    if (typeof BroadcastChannel !== "undefined") {
      this.channel = new BroadcastChannel("auth-sync");
      this.channel.onmessage = this.handleMessage.bind(this);
    }
  }

  private handleMessage(event: MessageEvent<AuthSyncMessage>) {
    const message = event.data;

    // Ignore messages older than the last processed one to handle rapid switches
    if (message.timestamp <= this.lastProcessedTimestamp) {
      return;
    }

    this.lastProcessedTimestamp = message.timestamp;

    // Notify all listeners
    this.listeners.forEach((listener) => {
      try {
        listener(message);
      } catch (error) {
        console.error("Error in auth sync listener:", error);
      }
    });
  }

  /**
   * Broadcast an authentication event to all tabs
   */
  broadcast(message: Omit<AuthSyncMessage, "timestamp">): void {
    if (!this.channel) {
      return;
    }

    const messageWithTimestamp: AuthSyncMessage = {
      ...message,
      timestamp: Date.now()
    } as AuthSyncMessage;

    try {
      this.channel.postMessage(messageWithTimestamp);
    } catch (error) {
      console.error("Failed to broadcast auth sync message:", error);
    }
  }

  /**
   * Subscribe to authentication events from other tabs
   */
  subscribe(listener: AuthSyncListener): () => void {
    this.listeners.add(listener);

    // Return unsubscribe function
    return () => {
      this.listeners.delete(listener);
    };
  }

  /**
   * Clean up resources
   */
  dispose(): void {
    if (this.channel) {
      this.channel.close();
      this.channel = null;
    }
    this.listeners.clear();
  }
}

// Export singleton instance
export const authSyncService = new AuthSyncService();
