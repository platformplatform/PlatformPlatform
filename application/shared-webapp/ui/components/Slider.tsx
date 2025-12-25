import { Slider as SliderPrimitive } from "@base-ui/react/slider";

import { cn } from "../utils";
import { Label } from "./Label";

export interface SliderProps extends Omit<SliderPrimitive.Root.Props, "defaultValue"> {
  label?: string;
  thumbLabels?: string[];
  defaultValue?: number | number[];
}

export function Slider({ label, thumbLabels, className, defaultValue, value, ...props }: Readonly<SliderProps>) {
  const normalizedDefaultValue =
    defaultValue !== undefined ? (Array.isArray(defaultValue) ? defaultValue : [defaultValue]) : undefined;
  const normalizedValue = value !== undefined ? (Array.isArray(value) ? value : [value]) : undefined;
  const isMultiple = (normalizedValue ?? normalizedDefaultValue ?? []).length > 1;

  return (
    <SliderPrimitive.Root
      data-slot="slider"
      defaultValue={normalizedDefaultValue}
      value={normalizedValue}
      className={cn(
        "group/slider grid w-64 grid-cols-[1fr_auto] items-center gap-2 data-[orientation=vertical]:flex data-[orientation=vertical]:flex-col",
        className
      )}
      {...props}
    >
      {label && <Label>{label}</Label>}
      <SliderPrimitive.Value className="font-medium text-muted-foreground text-sm group-data-[orientation=vertical]/slider:hidden">
        {(formattedValues) => formattedValues.join(" - ")}
      </SliderPrimitive.Value>
      <SliderPrimitive.Control
        className={cn(
          "col-span-2 flex items-center",
          "group-data-[orientation=horizontal]/slider:h-6",
          "group-data-[orientation=vertical]/slider:h-64 group-data-[orientation=vertical]/slider:w-6"
        )}
      >
        <SliderPrimitive.Track
          className={cn(
            "relative rounded-full",
            "group-data-[orientation=horizontal]/slider:h-2 group-data-[orientation=horizontal]/slider:w-full",
            "group-data-[orientation=vertical]/slider:ml-[50%] group-data-[orientation=vertical]/slider:h-full group-data-[orientation=vertical]/slider:w-2 group-data-[orientation=vertical]/slider:-translate-x-[50%]",
            isMultiple ? "bg-primary forced-colors:bg-[ButtonBorder]" : "bg-primary/40 forced-colors:bg-[ButtonBorder]",
            "group-data-[disabled]/slider:bg-muted group-data-[disabled]/slider:forced-colors:bg-[GrayText]"
          )}
        >
          <SliderPrimitive.Indicator
            className={cn(
              "absolute rounded-full bg-primary",
              "group-data-[orientation=horizontal]/slider:top-[50%] group-data-[orientation=horizontal]/slider:left-0 group-data-[orientation=horizontal]/slider:h-2 group-data-[orientation=horizontal]/slider:-translate-y-[50%]",
              "group-data-[orientation=vertical]/slider:bottom-0 group-data-[orientation=vertical]/slider:left-[50%] group-data-[orientation=vertical]/slider:w-2 group-data-[orientation=vertical]/slider:-translate-x-[50%]",
              "group-data-[disabled]/slider:bg-muted group-data-[disabled]/slider:forced-colors:bg-[GrayText]",
              isMultiple && "hidden"
            )}
          />
          {(normalizedValue ?? normalizedDefaultValue ?? [0]).map((_, index) => (
            <SliderPrimitive.Thumb
              key={index}
              aria-label={thumbLabels?.[index]}
              className={cn(
                "block size-6 rounded-full border-2 border-primary bg-background",
                "group-data-[orientation=horizontal]/slider:mt-6",
                "group-data-[orientation=vertical]/slider:ml-3",
                "focus-visible:outline focus-visible:outline-2 focus-visible:outline-ring focus-visible:outline-offset-2",
                "data-[dragging]:bg-accent data-[dragging]:forced-colors:bg-[ButtonBorder]",
                "group-data-[disabled]/slider:border-muted group-data-[disabled]/slider:forced-colors:border-[GrayText]"
              )}
            />
          ))}
        </SliderPrimitive.Track>
      </SliderPrimitive.Control>
    </SliderPrimitive.Root>
  );
}
