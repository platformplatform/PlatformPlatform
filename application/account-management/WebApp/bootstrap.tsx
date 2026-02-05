import "@repo/ui/theme.css";
import "@repo/ui/tailwind.css";
import { ApplicationInsightsProvider } from "@repo/infrastructure/applicationInsights/ApplicationInsightsProvider";
import { setupGlobalErrorHandlers } from "@repo/infrastructure/http/errorHandler";
import { Translation } from "@repo/infrastructure/translations/Translation";
import { Toaster } from "@repo/ui/components/Sonner";
import { RouterProvider } from "@tanstack/react-router";
import React from "react";
import reactDom from "react-dom/client";
import { router } from "@/shared/lib/router/router";

const { TranslationProvider } = await Translation.create(
  (locale) => import(`@/shared/translations/locale/${locale}.ts`)
);

const rootElement = document.getElementById("root");

if (!rootElement) {
  throw new Error("Root element not found.");
}

setupGlobalErrorHandlers();

reactDom.createRoot(rootElement).render(
  <React.StrictMode>
    <TranslationProvider>
      <ApplicationInsightsProvider>
        <RouterProvider router={router} />
        <Toaster position="top-center" closeButton={true} />
      </ApplicationInsightsProvider>
    </TranslationProvider>
  </React.StrictMode>
);
