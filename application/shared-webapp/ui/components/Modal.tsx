/**
 * ref: https://react-spectrum.adobe.com/react-aria-tailwind-starter/?path=/docs/alertdialog--docs
 * ref: https://ui.shadcn.com/docs/components/alert-dialog
 */
import { Modal as AriaModal, ModalOverlay, type ModalOverlayProps } from "react-aria-components";
import { tv } from "tailwind-variants";

const overlayStyles = tv({
  base: "fixed top-0 left-0 isolate flex h-[--visual-viewport-height] w-full bg-black/[15%] text-center",
  variants: {
    isEntering: {
      true: "fade-in animate-in duration-200 ease-out"
    },
    isExiting: {
      true: "fade-out animate-out duration-200 ease-in"
    },
    position: {
      center: "items-center justify-center",
      top: "items-start justify-center",
      left: "items-stretch justify-start",
      right: "items-stretch justify-end",
      bottom: "items-end justify-center"
    },
    fullSize: {
      true: "",
      false: "p-2 sm:p-4"
    },
    blur: {
      true: "backdrop-blur-lg",
      false: ""
    },
    zIndex: {
      normal: "z-[100]",
      high: "z-[300]"
    }
  },
  defaultVariants: {
    position: "center",
    fullSize: false,
    blur: true,
    zIndex: "normal"
  }
});

const modalStyles = tv({
  base: "flex max-h-[calc(100vh-2rem)] w-full flex-col overflow-hidden rounded-lg border border-border bg-popover bg-clip-padding text-left align-middle text-foreground shadow-2xl sm:w-fit dark:backdrop-blur-2xl dark:backdrop-saturate-200 forced-colors:bg-[Canvas]",
  variants: {
    isEntering: {
      true: "zoom-in-105 animate-in duration-200 ease-out"
    },
    isExiting: {
      true: "zoom-out-95 animate-out duration-200 ease-in"
    }
  }
});

type ModalProps = {
  position?: "center" | "top" | "left" | "right" | "bottom";
  fullSize?: boolean;
  blur?: boolean;
  zIndex?: "normal" | "high";
} & ModalOverlayProps;

export function Modal({ position, fullSize, blur, zIndex, ...props }: Readonly<ModalProps>) {
  return (
    <ModalOverlay
      {...props}
      className={(renderProps) => overlayStyles({ position, fullSize, blur, zIndex, ...renderProps })}
    >
      <AriaModal {...props} className={modalStyles} />
    </ModalOverlay>
  );
}
