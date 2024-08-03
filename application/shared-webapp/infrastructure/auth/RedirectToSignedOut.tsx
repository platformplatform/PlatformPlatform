import { Navigate } from "@tanstack/react-router";
import { signedOutPath } from "./constants";

export function RedirectToSignedOut() {
  return <Navigate to={signedOutPath} />;
}
