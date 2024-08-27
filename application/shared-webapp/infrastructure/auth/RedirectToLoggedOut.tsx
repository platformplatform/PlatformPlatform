import { Navigate } from "@tanstack/react-router";
import { loggedOutPath } from "./constants";

export function RedirectToLoggedOut() {
  return <Navigate to={loggedOutPath} />;
}
