import { withAITracking } from "@microsoft/applicationinsights-react-js";
import { ReactFilesystemRouter } from "@platformplatform/client-filesystem-router/react";
import { reactPlugin } from "./config";

export const ApplicationInsightsReactFilesystemRouter = withAITracking(reactPlugin, ReactFilesystemRouter);
