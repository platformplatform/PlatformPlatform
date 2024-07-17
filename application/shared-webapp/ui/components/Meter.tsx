/**
 * ref: https://react-spectrum.adobe.com/react-aria-tailwind-starter/?path=/docs/meter--docs
 */
import { AlertTriangle } from "lucide-react";
import { Meter as AriaMeter, type MeterProps as AriaMeterProps } from "react-aria-components";
import { Label } from "./Label";
import { composeTailwindRenderProps } from "./utils";

export interface MeterProps extends AriaMeterProps {
  label?: string;
  warnAt?: number;
  dangerAt?: number;
  showIndicators?: boolean;
}

export function Meter({ label, warnAt, dangerAt, showIndicators, ...props }: Readonly<MeterProps>) {
  return (
    <AriaMeter {...props} className={composeTailwindRenderProps(props.className, "flex flex-col gap-1")}>
      {({ percentage, valueText }) => (
        <>
          <div className="flex justify-between gap-2">
            <Label>{label}</Label>
            <span
              className={`text-sm ${valueExceeded(percentage, dangerAt) ? "text-danger" : "text-muted-foreground"}`}
            >
              {valueExceeded(percentage, dangerAt) && (
                <AlertTriangle aria-label="Alert" className="inline-block h-4 w-4 align-text-bottom" />
              )}
              {` ${valueText}`}
            </span>
          </div>
          <div className="-outline-offset-1 relative h-2 w-64 rounded-full bg-muted outline outline-1 outline-transparent">
            <div
              className={`absolute top-0 left-0 h-full rounded-full ${getTrackColor({ percentage, warnAt, dangerAt })} forced-colors:bg-[Highlight]`}
              style={{ width: `${percentage}%` }}
            />
            {showIndicators && warnAt != null && !valueExceeded(percentage, warnAt) && (
              <div
                className={"absolute top-0 left-0 h-full border-foreground border-r-2 border-r-warning"}
                style={{ width: `${warnAt}%` }}
              />
            )}
            {showIndicators && dangerAt != null && !valueExceeded(percentage, dangerAt) && (
              <div
                className={"absolute top-0 left-0 h-full border-foreground border-r-2 border-r-danger"}
                style={{ width: `${dangerAt}%` }}
              />
            )}
          </div>
        </>
      )}
    </AriaMeter>
  );
}

function valueExceeded(percentage: number, threshold?: number) {
  return threshold != null && percentage >= threshold;
}

type GetColorProps = {
  percentage: number;
  warnAt?: number;
  dangerAt?: number;
};

function getTrackColor({ percentage, warnAt, dangerAt }: GetColorProps) {
  if (valueExceeded(percentage, dangerAt)) {
    return "bg-danger";
  }
  if (valueExceeded(percentage, warnAt)) {
    return "bg-warning";
  }

  return "bg-success";
}
