import "@repo/ui/tailwind.css";
import { router } from "@/shared/lib/router/router";
import { ApplicationInsightsProvider } from "@repo/infrastructure/applicationInsights/ApplicationInsightsProvider";
import { Translation } from "@repo/infrastructure/translations/Translation";
import { RouterProvider } from "@tanstack/react-router";
import React from "react";
import ReactDOM from "react-dom/client";

const { TranslationProvider } = await Translation.create(
  (locale) => import(`@/shared/translations/locale/${locale}.ts`)
);

const rootElement = document.getElementById("root");

if (!rootElement) {
  throw new Error("Root element not found");
}

ReactDOM.createRoot(rootElement).render(
  <React.StrictMode>
    <TranslationProvider>
      <ApplicationInsightsProvider>
        <RouterProvider router={router} />
      </ApplicationInsightsProvider>
    </TranslationProvider>
  </React.StrictMode>
);
