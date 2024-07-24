import { createFileRoute } from "@tanstack/react-router";

export const Route = createFileRoute("/back-office/")({
  component: Home
});

export default function Home() {
  return <main className="flex w-full flex-col">Hello from Back Office</main>;
}
