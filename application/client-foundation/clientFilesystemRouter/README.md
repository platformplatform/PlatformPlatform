# Client Filesystem Router

A Client Filesystem Router is a tool that allows you to define routes in your application by simply creating files and directories in your project's source directory.

Build plugins are available for the following build tools:

- `rspack`

Filesystem routing styles / conventions supported:

- `nextjs/app` - NextJS app routing style (atm. partial support)

## Usage

### Installation

```bash
yarn i @platformplatform/client-filesystem-router -D
```

### Application entry point

```typescript
import React from "react";
import ReactDOM from "react-dom/client";
import { ReactFilesystemRouter } from "@platformplatform/client-filesystem-router/react";

ReactDOM.createRoot(document.getElementById("root")!).render(
  <React.StrictMode>
    <ReactFilesystemRouter />
  </React.StrictMode>
);
```

### RSPack Plugin

```typescript
import { ClientFilesystemRouterPlugin } from "@platformplatform/client-filesystem-router/rspack-plugin";

// ...

const configuration: Configuration = {
  plugins: [
    // ...
    new ClientFilesystemRouterPlugin({
      dir: "src/app", // The directory to scan for filesystem routes
    }),
  ],
};
```

_(Note: If the build plugin is not configured you should see an error message "Please configure ClientFilesystemRouterPlugin in your build config.")_

### NextJS App Routing Style

#### Example

```
app
├── layout.tsx
├── page.tsx           (Home page)
├── error.tsx          (Error page)
├── not-found.tsx      (404 page)
├── invoices
│   ├── [id]           (dynamic route)
│   │   ├── page.tsx   (Page with param.id set)
│   │   └── not-found.tsx
│   └── page.tsx
└── slow
    ├── page.tsx       (lazy loaded page)
    └── loading.tsx    (loading fallback)
```
