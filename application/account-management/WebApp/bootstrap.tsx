import "@repo/ui/tailwind.css";
import { router } from "@/shared/lib/router/router";
import { ApplicationInsightsProvider } from "@repo/infrastructure/applicationInsights/ApplicationInsightsProvider";
import { initializeHttpInterceptors } from "@repo/infrastructure/http/antiforgeryTokenHandler";
import { Translation } from "@repo/infrastructure/translations/Translation";
import { GlobalToastRegion } from "@repo/ui/components/Toast";
import { RouterProvider } from "@tanstack/react-router";
import React from "react";
import reactDom from "react-dom/client";

// Initialize HTTP interceptors to automatically handle antiforgery tokens
initializeHttpInterceptors();

const { TranslationProvider } = await Translation.create(
  (locale) => import(`@/shared/translations/locale/${locale}.ts`)
);

const rootElement = document.getElementById("root");

if (!rootElement) {
  throw new Error("Root element not found.");
}

reactDom.createRoot(rootElement).render(
  <React.StrictMode>
    <TranslationProvider>
      <ApplicationInsightsProvider>
        <RouterProvider router={router} />
        <GlobalToastRegion />
      </ApplicationInsightsProvider>
    </TranslationProvider>
  </React.StrictMode>
);
