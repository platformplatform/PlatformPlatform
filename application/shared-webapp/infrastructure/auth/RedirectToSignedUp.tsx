import { Navigate } from "@tanstack/react-router";
import { signedUpPath } from "./constants";

export function RedirectToSignedUp() {
  return <Navigate to={signedUpPath} />;
}
