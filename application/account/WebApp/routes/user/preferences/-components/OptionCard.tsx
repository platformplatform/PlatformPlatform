import { Button } from "@repo/ui/components/Button";
import { CheckIcon } from "lucide-react";

export function OptionCard({
  icon,
  label,
  isSelected,
  onClick
}: Readonly<{
  icon: React.ReactNode;
  label: string;
  isSelected: boolean;
  onClick: () => void;
}>) {
  return (
    <Button
      variant="outline"
      aria-pressed={isSelected}
      onClick={onClick}
      className={`h-auto w-full justify-start gap-3 rounded-lg p-4 font-normal ${
        isSelected ? "border-primary bg-primary/5 hover:bg-primary/5 active:bg-primary/10" : "active:bg-accent"
      }`}
    >
      <span className="text-muted-foreground">{icon}</span>
      <span className="flex-1 text-left text-sm">{label}</span>
      {isSelected && <CheckIcon className="size-4 text-primary" />}
    </Button>
  );
}
