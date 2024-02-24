import { i18n } from "@lingui/core";
import { I18nProvider } from "@lingui/react";
import React from "react";
import ReactDOM from "react-dom/client";
import { ApplicationInsightsProvider } from "./lib/applicationInsights/ApplicationInsightsProvider";
import "./main.css";
import { dynamicActivate, getInitialLocale } from "./translations/i18n";
import { ReactFilesystemRouter } from "@/lib/router/router";

await dynamicActivate(i18n, getInitialLocale());

ReactDOM.createRoot(document.getElementById("root")!).render(
  <React.StrictMode>
    <I18nProvider i18n={i18n}>
      <ApplicationInsightsProvider>
        <ReactFilesystemRouter />
      </ApplicationInsightsProvider>
    </I18nProvider>
  </React.StrictMode>
);
