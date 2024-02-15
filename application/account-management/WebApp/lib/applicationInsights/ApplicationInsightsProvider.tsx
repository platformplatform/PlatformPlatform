import { AppInsightsContext, AppInsightsErrorBoundary } from "@microsoft/applicationinsights-react-js";
import type { ReactNode } from "react";
import { ErrorFallback } from "./ErrorFallback.tsx";
import { reactPlugin } from "./config";

export interface AppInsightsProviderProps {
  children: ReactNode;
}
export function ApplicationInsightsProvider({ children }: Readonly<AppInsightsProviderProps>) {
  return (
    <AppInsightsErrorBoundary onError={ErrorFallback} appInsights={reactPlugin}>
      <AppInsightsContext.Provider value={reactPlugin}>{children}</AppInsightsContext.Provider>
    </AppInsightsErrorBoundary>
  );
}
