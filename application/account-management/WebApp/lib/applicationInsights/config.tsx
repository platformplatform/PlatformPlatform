import { ReactPlugin } from "@microsoft/applicationinsights-react-js";
import { ApplicationInsights } from "@microsoft/applicationinsights-web";

export const reactPlugin = new ReactPlugin();

const applicationInsights = new ApplicationInsights({
  config: {
    // Set the application ID to the webapp and application
    appId: "account-management/webapp",
    // Set the instrumentation key to a dummy value as we are not using the default endpoint
    instrumentationKey: "webapp",
    disableInstrumentationKeyValidation: true,
    // Set the endpoint URL to our custom endpoint
    endpointUrl: "/api/track",
    // Enable auto route tracking for React Router
    enableAutoRouteTracking: true,
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
  },
});

// Load the Application Insights script
applicationInsights.loadAppInsights();
// Track the initial page view
applicationInsights.trackPageView();
