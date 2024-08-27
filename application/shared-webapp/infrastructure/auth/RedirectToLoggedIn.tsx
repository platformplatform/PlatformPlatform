import { Navigate } from "@tanstack/react-router";
import { loggedInPath } from "./constants";

export function RedirectToLoggedIn() {
  return <Navigate to={loggedInPath} />;
}
