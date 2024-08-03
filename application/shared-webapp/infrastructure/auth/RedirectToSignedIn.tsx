import { Navigate } from "@tanstack/react-router";
import { signedInPath } from "./constants";

export function RedirectToSignedIn() {
  return <Navigate to={signedInPath} />;
}
