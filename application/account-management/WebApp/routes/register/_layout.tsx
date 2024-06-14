import { Outlet, createFileRoute } from "@tanstack/react-router";

export const Route = createFileRoute("/register/_layout")({
  component: LoginLayout,
});

export function LoginLayout() {
  return (
    <Outlet />
  );
}
