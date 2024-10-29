import { loggedInPath, loggedOutPath, signedUpPath, loginPath, logoutPath, signUpPath } from "../auth/constants";

export const additionalRoutes = [
  "https://",
  "http://",
  "/",
  loginPath,
  logoutPath,
  signUpPath,
  loggedOutPath,
  loggedInPath,
  signedUpPath
] as const;

export type AdditionalRoutes = (typeof additionalRoutes)[number];
