import { NotFoundError } from "@repo/infrastructure/auth/routeGuards";
import { createFileRoute } from "@tanstack/react-router";

/**
 * Catch-all route for unmatched paths under /user.
 * Throws NotFoundError in beforeLoad so the error boundary renders
 * a full-page 404 without the side menu.
 */
export const Route = createFileRoute("/user/$")({
  beforeLoad: () => {
    throw new NotFoundError();
  }
});
