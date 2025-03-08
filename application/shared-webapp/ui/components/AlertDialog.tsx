import { AlertCircleIcon, InfoIcon } from "lucide-react";
import { type ReactNode, useId } from "react";
import { chain } from "react-aria";
import type { DialogProps } from "react-aria-components";
import { tv } from "tailwind-variants";
import { Button } from "./Button";
/**
 * ref: https://react-spectrum.adobe.com/react-aria-tailwind-starter/?path=/docs/alertdialog--docs
 * ref: https://ui.shadcn.com/docs/components/alert-dialog
 */
import { Dialog } from "./Dialog";
import { Heading } from "./Heading";

interface AlertDialogProps extends Omit<DialogProps, "children"> {
  title: string;
  children: ReactNode;
  variant?: "info" | "destructive";
  actionLabel?: string;
  cancelLabel?: string;
  onAction?: () => void;
}

const alertDialogContents = tv({
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
  ...props
}: Readonly<AlertDialogProps>) {
  const contentId = useId();
  return (
    <Dialog role="alertdialog" aria-describedby={contentId} {...props}>
      {({ close }) => (
        <>
          <Heading slot="title">{title}</Heading>
          <div className={alertDialogContents({ variant })}>
            {variant === "destructive" ? <AlertCircleIcon aria-hidden={true} /> : <InfoIcon aria-hidden={true} />}
          </div>
          <div id={contentId}>{children}</div>
          {actionLabel && (
            <fieldset className="flex justify-end gap-2 pt-10">
              <Button variant="secondary" onPress={close}>
                {cancelLabel ?? "Cancel"}
              </Button>
              <Button
                variant={variant === "destructive" ? "destructive" : "primary"}
                autoFocus={true}
                onPress={chain(onAction, close)}
              >
                {actionLabel}
              </Button>
            </fieldset>
          )}
        </>
      )}
    </Dialog>
  );
}
