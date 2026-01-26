import "@repo/ui/tailwind.css";

export interface AccountAppProps {
  initialPath: string;
  onNavigateToMain: (path: string) => void;
}

export default function AccountApp({ initialPath }: AccountAppProps) {
  return (
    <div id="account" style={{ minHeight: "100vh" }} className="flex w-full flex-col bg-background">
      <div className="flex flex-1 flex-col items-center justify-center">
        <p className="text-muted-foreground">AccountApp placeholder - will be implemented in next task</p>
        <p className="text-muted-foreground text-sm">Path: {initialPath}</p>
      </div>
    </div>
  );
}
