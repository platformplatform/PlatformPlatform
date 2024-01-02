import { ReactPlugin } from "@microsoft/applicationinsights-react-js";
import { ApplicationInsights } from "@microsoft/applicationinsights-web";

export const reactPlugin = new ReactPlugin();
const appInsights = new ApplicationInsights({
  config: {
    // accountId: tenantId, // subdomain, cookie or custom domain
    appId: "account-management/webapp",
    connectionString: import.meta.env.APP_INSIGHTS_CONNECTION_STRING,
    enableAutoRouteTracking: true,
    extensions: [reactPlugin],
  },
});

// Set additional properties
appInsights.context.application.ver = import.meta.env.APPLICATION_VERSION;
// appInsights.context.user.id = userId;
// appInsights.context.session.acquisitionDate

// Set custom properties
/* appInsights.trackEvent({ name: "App started" }, {
  // Custom properties
  "system-cluster": "prod-westeurope",
  "tenant-state": "trial",
  "user-role": "admin",
}); */

// Load the Application Insights script
appInsights.loadAppInsights();
