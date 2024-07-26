/**
 * ref: https://react-spectrum.adobe.com/react-aria-tailwind-starter/?path=/docs/alertdialog--docs
 * ref: https://ui.shadcn.com/docs/components/alert-dialog
 */
import { Modal as AriaModal, ModalOverlay, type ModalOverlayProps } from "react-aria-components";
import { tv } from "tailwind-variants";

const overlayStyles = tv({
  base: "fixed top-0 left-0 w-full h-[--visual-viewport-height] isolate z-20 bg-black/[15%] text-center flex backdrop-blur-lg",
  variants: {
    isEntering: {
      true: "animate-in fade-in duration-200 ease-out"
    },
    isExiting: {
      true: "animate-out fade-out duration-200 ease-in"
    },
    position: {
      center: "items-center justify-center",
      top: "items-start justify-center",
      left: "justify-start items-stretch",
      right: "justify-end items-stretch",
      bottom: "items-end justify-center"
    },
    fullSize: {
      true: "",
      false: "p-4"
    }
  },
  defaultVariants: {
    position: "center",
    fullSize: false
  }
});

const modalStyles = tv({
  base: "w-fit rounded-lg bg-popover dark:backdrop-blur-2xl dark:backdrop-saturate-200 forced-colors:bg-[Canvas] text-left align-middle text-foreground shadow-2xl bg-clip-padding border border-border",
  variants: {
    isEntering: {
      true: "animate-in zoom-in-105 ease-out duration-200"
    },
    isExiting: {
      true: "animate-out zoom-out-95 ease-in duration-200"
    }
  }
});

type ModalProps = {
  position?: "center" | "top" | "left" | "right" | "bottom";
  fullSize?: boolean;
} & ModalOverlayProps;

export function Modal({ position, fullSize, ...props }: Readonly<ModalProps>) {
  return (
    <ModalOverlay {...props} className={(renderProps) => overlayStyles({ position, fullSize, ...renderProps })}>
      <AriaModal {...props} className={modalStyles} />
    </ModalOverlay>
  );
}
