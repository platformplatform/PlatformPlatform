import { Button } from "@repo/ui/components/Button";

interface CsatSmileyButtonProps {
  emoji: string;
  label: string;
  selected: boolean;
  disabled: boolean;
  onSelect: () => void;
}

// A single CSAT rating choice rendered as a large round emoji toggle.
export function CsatSmileyButton({ emoji, label, selected, disabled, onSelect }: Readonly<CsatSmileyButtonProps>) {
  return (
    <div className="flex flex-col items-center gap-1.5">
      <Button
        type="button"
        variant="outline"
        size="icon"
        onClick={onSelect}
        disabled={disabled}
        className={`size-12 rounded-full text-2xl ${selected ? "border-primary bg-primary/10 hover:bg-primary/10" : ""}`}
        aria-pressed={selected}
        aria-label={label}
      >
        {emoji}
      </Button>
      <span className="text-xs text-muted-foreground">{label}</span>
    </div>
  );
}
