export type DialogSize = "sm" | "md" | "lg" | "xl" | "2xl";

const dialogSizeClassNames: Record<DialogSize, string> = {
  sm: "sm:w-dialog-sm",
  md: "sm:w-dialog-md",
  lg: "sm:w-dialog-lg",
  xl: "sm:w-dialog-xl",
  "2xl": "sm:w-dialog-2xl"
};

export function getDialogSizeClassName(size: DialogSize): string {
  return dialogSizeClassNames[size];
}
