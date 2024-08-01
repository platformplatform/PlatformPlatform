import { createFileRoute } from "@tanstack/react-router";
import { lazy } from "react";

const AvatarButton = lazy(() => import("account-management/AvatarButton"));

export const Route = createFileRoute("/back-office/")({
  component: Home
});

export default function Home() {
  return (
    <main className="flex w-full flex-col">
      Hello from Back Office <AvatarButton />
    </main>
  );
}
