import React from "react";
import ReactDOM from "react-dom/client";
import { I18nProvider } from "@lingui/react";
import { i18n } from "@lingui/core";
import "./main.css";
import { ReactFilesystemRouter } from "@platformplatform/client-filesystem-router/react";
import { dynamicActivate, getInitialLocale } from "./translations/i18n";

await dynamicActivate(i18n, getInitialLocale());

ReactDOM.createRoot(document.getElementById("root")!).render(
  <React.StrictMode>
    <I18nProvider i18n={i18n}>
      <ReactFilesystemRouter />
    </I18nProvider>
  </React.StrictMode>
);
