import { Navigate } from "@tanstack/react-router";
import { signUpPath } from "./constants";

export function RedirectToSignUp() {
  return <Navigate to={signUpPath} />;
}
