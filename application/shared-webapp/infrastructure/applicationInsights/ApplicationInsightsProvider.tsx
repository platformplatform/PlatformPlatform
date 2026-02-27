import { AppInsightsContext, AppInsightsErrorBoundary, ReactPlugin } from "@microsoft/applicationinsights-react-js";
import { ApplicationInsights } from "@microsoft/applicationinsights-web";
import { type ReactNode, useEffect, useRef } from "react";
import { useUserInfo } from "../auth/hooks";

const reactPlugin = new ReactPlugin();
const ErrorFallback = () => <h1>Something went wrong, please try again</h1>;

export interface AppInsightsProviderProps {
  children: ReactNode;
}

export function ApplicationInsightsProvider({ children }: Readonly<AppInsightsProviderProps>) {
  const userInfo = useUserInfo();
  if (userInfo?.isAuthenticated) {
    applicationInsights.setAuthenticatedUserContext(userInfo.id as string, userInfo.tenantId as string, true);
  } else {
    applicationInsights.clearAuthenticatedUserContext();
  }

  return (
    <AppInsightsErrorBoundary onError={ErrorFallback} appInsights={reactPlugin}>
      <AppInsightsContext.Provider value={reactPlugin}>{children}</AppInsightsContext.Provider>
    </AppInsightsErrorBoundary>
  );
}

const applicationInsights = new ApplicationInsights({
  config: {
    // Set the application ID to the webapp and application
    appId: import.meta.build_env.applicationId,
    // Set the instrumentation key to a dummy value as we are not using the default endpoint
    instrumentationKey: "webapp",
    disableInstrumentationKeyValidation: true,
    // Set the endpoint URL to our custom endpoint
    endpointUrl: "/api/track",
    // Disable auto route tracking (not compatible with TanStack Router)
    enableAutoRouteTracking: false,
    // Instrument error tracking
    autoExceptionInstrumented: true,
    autoUnhandledPromiseInstrumented: true,
    // Disable the page view tracking as we are not using the metrics
    autoTrackPageVisitTime: false,
    // Disable the beacon API we only support the fetch API
    isBeaconApiDisabled: true,
    extensions: [reactPlugin],
    // Disable dependency tracking
    disableFetchTracking: true,
    disableAjaxTracking: true,
    // Disable the unload event as it is not best practice
    disablePageUnloadEvents: ["unload"],
    extensionConfig: {
      AppInsightsCfgSyncPlugin: {
        // this will block fetching from default cdn
        cfgUrl: ""
      }
    }
  }
});

// Load the Application Insights script
applicationInsights.loadAppInsights();

export type TrackingType = "page" | "menu" | "dialog" | "sidepane" | "interaction";
export type TrackingAction = "open" | "close" | "submit" | "cancel" | "confirm";

export function trackInteraction(
  name: string,
  type: "page" | "interaction",
  action?: string,
  extraProperties?: Record<string, string>
): void;
export function trackInteraction(
  name: string,
  type: "menu" | "dialog" | "sidepane",
  action: TrackingAction,
  extraProperties?: Record<string, string>
): void;
export function trackInteraction(
  name: string,
  type: TrackingType,
  action?: string,
  extraProperties?: Record<string, string>
) {
  applicationInsights.trackPageView({
    name: action ? `${name} - ${action}${type === "interaction" ? "" : ` ${type}`}` : name,
    uri: window.location.href,
    properties: { type, ...extraProperties }
  });
}

// Register on window for cross-module-federation access.
// Components in @repo/ui cannot import from @repo/infrastructure due to rootDir boundaries,
// so they call this via the window global instead.
(window as unknown as { __trackInteraction: typeof trackInteraction }).__trackInteraction = trackInteraction;

export function useTrackOpen(name: string, type: "menu" | "dialog" | "sidepane", isOpen = true, key?: string) {
  const prevOpen = useRef(false);
  const prevKey = useRef(key);
  useEffect(() => {
    const opened = isOpen && !prevOpen.current;
    const contentChanged = isOpen && prevOpen.current && key !== undefined && key !== prevKey.current;
    if (opened || contentChanged) {
      trackInteraction(name, type, "open");
    }
    prevOpen.current = isOpen;
    prevKey.current = key;
  }, [isOpen, name, type, key]);
}

export function useTrackClose(name: string, type: "menu" | "dialog" | "sidepane", isOpen = true) {
  const prevOpen = useRef(false);
  useEffect(() => {
    if (!isOpen && prevOpen.current) {
      trackInteraction(name, type, "close");
    }
    prevOpen.current = isOpen;
  }, [isOpen, name, type]);
}

// Export for error tracking
export { applicationInsights };
