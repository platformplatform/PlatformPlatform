import "@repo/ui/tailwind.css";
import { router } from "@/lib/router/router";
import { i18n } from "@lingui/core";
import { I18nProvider } from "@lingui/react";
import { RouterProvider } from "@tanstack/react-router";
import React from "react";
import ReactDOM from "react-dom/client";
import { ApplicationInsightsProvider } from "./lib/applicationInsights/ApplicationInsightsProvider";
import { dynamicActivate, getInitialLocale } from "./translations/i18n";

await dynamicActivate(i18n, getInitialLocale());

const rootElement = document.getElementById("root");

if (!rootElement) {
  throw new Error("Root element not found");
}

ReactDOM.createRoot(rootElement).render(
  <React.StrictMode>
    <I18nProvider i18n={i18n}>
      <ApplicationInsightsProvider>
        <RouterProvider router={router} />
      </ApplicationInsightsProvider>
    </I18nProvider>
  </React.StrictMode>
);
