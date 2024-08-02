import { signedInPath, signedOutPath, signedUpPath, signInPath, signOutPath, signUpPath } from "../auth/constants";

export const additionalRoutes = [
  "https://",
  "http://",
  "/",
  signInPath,
  signOutPath,
  signUpPath,
  signedOutPath,
  signedInPath,
  signedUpPath
] as const;

export type AdditionalRoutes = (typeof additionalRoutes)[number];
