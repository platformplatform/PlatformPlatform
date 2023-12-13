import React from "react";
import ReactDOM from "react-dom/client";

import "./main.css";
import { ReactFilesystemRouter } from "@platformplatform/client-filesystem-router/react";
import { TranslationProvider } from "./translations/TranslationProvider";

ReactDOM.createRoot(document.getElementById("root")!).render(
  <React.StrictMode>
    <TranslationProvider>
      <ReactFilesystemRouter />
    </TranslationProvider>
  </React.StrictMode>
);
