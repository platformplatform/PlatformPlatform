import { ReactPlugin } from "@microsoft/applicationinsights-react-js";
import { ApplicationInsights } from "@microsoft/applicationinsights-web";

export const reactPlugin = new ReactPlugin();

const appInsights = new ApplicationInsights({
  config: {
    appId: "account-management/webapp",
    instrumentationKey: "webapp",
    disableInstrumentationKeyValidation: true,
    endpointUrl: "/api/track",
    enableAutoRouteTracking: true,
    autoExceptionInstrumented: true,
    autoUnhandledPromiseInstrumented: true,
    autoTrackPageVisitTime: false,
    isBeaconApiDisabled: true,
    extensions: [reactPlugin],
    disableFetchTracking: true,
    disableAjaxTracking: true,
  },
});

// Load the Application Insights script
appInsights.loadAppInsights();
appInsights.trackPageView();
