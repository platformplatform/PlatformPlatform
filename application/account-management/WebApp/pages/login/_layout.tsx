import { createFileRoute, Outlet } from "@tanstack/react-router";

export const Route = createFileRoute("/login/_layout")({
  component: LoginLayout
});

export default function LoginLayout() {
  return <Outlet />;
}
