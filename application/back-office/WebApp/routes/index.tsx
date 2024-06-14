import { createFileRoute } from "@tanstack/react-router";

export const Route = createFileRoute("/")({
  component: LandingPage,
});

export default function LandingPage() {
  return (
    <main className="flex w-full flex-col">
      Hello from Back Office
    </main>
  );
}
