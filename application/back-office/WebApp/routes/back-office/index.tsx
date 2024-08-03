import { createFileRoute } from "@tanstack/react-router";
import { lazy } from "react";

const UserButton = lazy(() => import("account-management/UserButton"));

export const Route = createFileRoute("/back-office/")({
  component: Home
});

export default function Home() {
  return (
    <main className="flex w-full flex-col">
      Hello from Back Office <UserButton />
    </main>
  );
}
