import { AppInsightsContext, AppInsightsErrorBoundary } from "@microsoft/applicationinsights-react-js";
import { ReactNode } from "react";
import { AppErrorFallback } from "./AppErrorFallback";
import { reactPlugin } from "./config";

export interface AppInsightsProviderProps {
  children: ReactNode;
}
export const AppInsightsProvider = ({ children }: AppInsightsProviderProps) => (
  <AppInsightsErrorBoundary onError={AppErrorFallback} appInsights={reactPlugin}>
    <AppInsightsContext.Provider value={reactPlugin}>{children}</AppInsightsContext.Provider>
  </AppInsightsErrorBoundary>
);
