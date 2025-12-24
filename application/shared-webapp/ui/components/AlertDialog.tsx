import { AlertCircleIcon, InfoIcon } from "lucide-react";
import { type ReactNode, useId, useState } from "react";
import { twMerge } from "tailwind-merge";
import { tv } from "tailwind-variants";
import { Button } from "./Button";
import { Dialog, DialogClose, DialogContent, DialogTitle } from "./Dialog";

interface AlertDialogProps {
  title: string;
  children: ReactNode;
  variant?: "info" | "destructive";
  actionLabel?: string;
  cancelLabel?: string;
  onAction?: () => void;
  className?: string;
}

const alertDialogIconStyles = tv({
  base: "absolute top-6 right-6 h-6 w-6 stroke-2",
  variants: {
    variant: {
      neutral: "hidden",
      destructive: "text-destructive",
      info: "text-primary"
    }
  },
  defaultVariants: {
    variant: "neutral"
  }
});

export function AlertDialog({
  title,
  variant,
  cancelLabel,
  actionLabel,
  onAction,
  children,
  className
}: Readonly<AlertDialogProps>) {
  const contentId = useId();
  const [isPending, setIsPending] = useState(false);

  const handleAction = async () => {
    if (onAction) {
      setIsPending(true);
      try {
        await onAction();
      } finally {
        setIsPending(false);
      }
    }
  };

  return (
    <DialogContent
      role="alertdialog"
      aria-describedby={contentId}
      showCloseButton={false}
      className={twMerge("sm:w-dialog-md", className)}
    >
      <DialogTitle>{title}</DialogTitle>
      <div className={alertDialogIconStyles({ variant })}>
        {variant === "destructive" ? <AlertCircleIcon aria-hidden={true} /> : <InfoIcon aria-hidden={true} />}
      </div>
      <div id={contentId}>{children}</div>
      {actionLabel && (
        <fieldset className="flex justify-end gap-2 pt-10">
          <DialogClose render={<Button variant="secondary" disabled={isPending} />}>
            {cancelLabel ?? "Cancel"}
          </DialogClose>
          <Button
            variant={variant === "destructive" ? "destructive" : "default"}
            autoFocus={true}
            disabled={isPending}
            onClick={handleAction}
          >
            {actionLabel}
          </Button>
        </fieldset>
      )}
    </DialogContent>
  );
}

export { Dialog as AlertDialogRoot };
