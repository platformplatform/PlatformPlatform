import { createRouter } from "@tanstack/react-router";
import { routeTree } from "./routeTree.generated";

// Set up a Router instance
export const router = createRouter({
  routeTree,
  defaultPreload: "intent"
});

// Register router with tanstack/react-router
declare module "@tanstack/react-router" {
  interface Register {
    router: typeof router;
  }
}
