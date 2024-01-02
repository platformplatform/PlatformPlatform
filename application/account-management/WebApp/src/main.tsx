import { i18n } from "@lingui/core";
import { I18nProvider } from "@lingui/react";
import React from "react";
import ReactDOM from "react-dom/client";
import { AppInsightsProvider } from "./lib/appInsights/AppInsightsProvider";
import { AppInsightsReactFilesystemRouter } from "./lib/appInsights/AppInsightsReactFilesystemRouter";
import "./main.css";
import { dynamicActivate, getInitialLocale } from "./translations/i18n";

await dynamicActivate(i18n, getInitialLocale());

ReactDOM.createRoot(document.getElementById("root")!).render(
  <React.StrictMode>
    <I18nProvider i18n={i18n}>
      <AppInsightsProvider>
        <AppInsightsReactFilesystemRouter />
      </AppInsightsProvider>
    </I18nProvider>
  </React.StrictMode>
);
