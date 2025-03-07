/**
 * ref: https://react-spectrum.adobe.com/react-aria/DropZone.html
 * ref: https://ui.shadcn.com/docs/components/input
 */
import { DropZone as AriaDropZone, composeRenderProps } from "react-aria-components";
import type { DropZoneProps } from "react-aria-components";
import { tv } from "tailwind-variants";

const dropZoneStyles = tv({
  base: "flex h-[150px] w-[300px] flex-col items-center justify-center gap-2 rounded-md border border-dashed text-sm data-[drop-target]:border-primary data-[drop-target]:border-solid data-[drop-target]:bg-accent"
});

export function DropZone({ className, ...props }: Readonly<DropZoneProps>) {
  return (
    <AriaDropZone
      {...props}
      className={composeRenderProps(className, (className, renderProps) =>
        dropZoneStyles({ ...renderProps, className })
      )}
    />
  );
}
