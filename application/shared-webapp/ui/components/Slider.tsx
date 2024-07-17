/**
 * ref: https://react-spectrum.adobe.com/react-aria-tailwind-starter/?path=/docs/slider--docs
 * ref: https://ui.shadcn.com/docs/components/slider
 */
import {
  Slider as AriaSlider,
  type SliderProps as AriaSliderProps,
  SliderOutput,
  SliderThumb,
  SliderTrack
} from "react-aria-components";
import { tv } from "tailwind-variants";
import { Label } from "./Label";
import { focusRing } from "./focusRing";
import { composeTailwindRenderProps } from "./utils";

const trackStyles = tv({
  base: "rounded-full bg-white",
  variants: {
    orientation: {
      horizontal: "w-full h-2",
      vertical: "h-full w-2 ml-[50%] -translate-x-[50%]"
    },
    isMultiple: {
      true: "bg-primary forced-colors:bg-[ButtonBorder]",
      false: "bg-primary/40 forced-colors:bg-[ButtonBorder]"
    },
    isDisabled: {
      true: "bg-muted forced-colors:bg-[GrayText]"
    }
  }
});

const fillStyles = tv({
  base: "absolute rounded-full bg-primary",
  variants: {
    orientation: {
      horizontal: "top-[50%] h-2 translate-y-[-50%] left-0",
      vertical: "left-[50%] w-2 translate-x-[-50%] bottom-0"
    },
    isDisabled: {
      true: "bg-muted forced-colors:bg-[GrayText]"
    },
    isMultiple: {
      true: "hidden"
    }
  }
});

const thumbStyles = tv({
  extend: focusRing,
  base: "w-6 h-6 group-orientation-horizontal:mt-6 group-orientation-vertical:ml-3 rounded-full bg-background border-2 border-primary",
  variants: {
    isDragging: {
      true: "bg-accent forced-colors:bg-[ButtonBorder]"
    },
    isDisabled: {
      true: "border-muted forced-colors:border-[GrayText]"
    }
  }
});

export interface SliderProps<T> extends AriaSliderProps<T> {
  label?: string;
  thumbLabels?: string[];
}

export function Slider<T extends number | number[]>({ label, thumbLabels, ...props }: Readonly<SliderProps<T>>) {
  return (
    <AriaSlider
      {...props}
      className={composeTailwindRenderProps(
        props.className,
        "orientation-vertical:flex orientation-horizontal:grid orientation-horizontal:w-64 grid-cols-[1fr_auto] flex-col items-center gap-2"
      )}
    >
      <Label>{label}</Label>
      <SliderOutput className="orientation-vertical:hidden font-medium text-muted-foreground text-sm">
        {({ state }) => state.values.map((_, i) => state.getThumbValueLabel(i)).join(" - ")}
      </SliderOutput>
      <SliderTrack className="group col-span-2 flex orientation-horizontal:h-6 orientation-vertical:h-64 orientation-vertical:w-6 items-center">
        {({ state, ...renderProps }) => (
          <>
            {/* track */}
            <div
              className={trackStyles({
                ...renderProps,
                isMultiple: state.values.length > 1
              })}
            />
            {/* fill */}
            <div
              className={fillStyles({
                ...renderProps,
                isMultiple: state.values.length > 1
              })}
              style={
                state.orientation === "horizontal"
                  ? { width: `${state.getThumbPercent(0) * 100}%` }
                  : { height: `${state.getThumbPercent(0) * 100}%` }
              }
            />
            {state.values.map((_, i) => (
              <SliderThumb
                key={`_slider_thumb_${i.toString()}`}
                index={i}
                aria-label={thumbLabels?.[i]}
                className={thumbStyles}
              />
            ))}
          </>
        )}
      </SliderTrack>
    </AriaSlider>
  );
}
