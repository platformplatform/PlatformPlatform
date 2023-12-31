import { i18n } from "@lingui/core";
import { Trans } from "@lingui/macro";
import { I18nProvider } from "@lingui/react";
import {
  AppInsightsContext,
  AppInsightsErrorBoundary,
  ReactPlugin,
  withAITracking,
} from "@microsoft/applicationinsights-react-js";
import { ApplicationInsights } from "@microsoft/applicationinsights-web";
import { ReactFilesystemRouter } from "@platformplatform/client-filesystem-router/react";
import React from "react";
import ReactDOM from "react-dom/client";
import "./main.css";
import { dynamicActivate, getInitialLocale } from "./translations/i18n";

const reactPlugin = new ReactPlugin();
const appInsights = new ApplicationInsights({
  config: {
    connectionString: import.meta.env.APP_INSIGHTS_CONNECTION_STRING,
    enableAutoRouteTracking: true,
    extensions: [reactPlugin],
  },
});

appInsights.loadAppInsights();

await dynamicActivate(i18n, getInitialLocale());

const AppReactFilesystemRouter = withAITracking(reactPlugin, ReactFilesystemRouter);
const AppErrorFallback = () => (
  <h1>
    <Trans>Something went wrong, please try again</Trans>
  </h1>
);

ReactDOM.createRoot(document.getElementById("root")!).render(
  <React.StrictMode>
    <I18nProvider i18n={i18n}>
      <AppInsightsErrorBoundary onError={AppErrorFallback} appInsights={reactPlugin}>
        <AppInsightsContext.Provider value={reactPlugin}>
          <AppReactFilesystemRouter />
        </AppInsightsContext.Provider>
      </AppInsightsErrorBoundary>
    </I18nProvider>
  </React.StrictMode>
);
