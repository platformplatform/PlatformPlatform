import { Navigate } from "@tanstack/react-router";
import { signOutPath } from "./constants";

export function RedirectToSignOut() {
  return <Navigate to={signOutPath} />;
}
