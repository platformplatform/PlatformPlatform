import { Link } from "@repo/ui/components/Link";
import { additionalRoutes } from "../router/additionalRoutes";

export function NotFound() {
  if (additionalRoutes.some((prefix) => window.location.pathname.startsWith(prefix))) {
    // Reload the page to navigate to the external link
    window.location.reload();
    return null;
  }
  return (
    <div>
      <h2>Not Found</h2>
      <p>Could not find requested resource</p>
      <Link href="/">Return Home</Link>
    </div>
  );
}
