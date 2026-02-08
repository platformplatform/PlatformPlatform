import { OTPInput, OTPInputContext } from "input-otp";
import { MinusIcon } from "lucide-react";
import * as React from "react";
import { cn } from "../utils";

function InputOtp({
  className,
  containerClassName,
  ...props
}: React.ComponentProps<typeof OTPInput> & {
  containerClassName?: string;
}) {
  return (
    <OTPInput
      data-slot="input-otp"
      containerClassName={cn("cn-input-otp flex items-center has-disabled:opacity-50", containerClassName)}
      spellCheck={false}
      className={cn("disabled:cursor-not-allowed", className)}
      {...props}
    />
  );
}

function InputOtpGroup({ className, ...props }: React.ComponentProps<"div">) {
  return (
    <div
      data-slot="input-otp-group"
      className={cn(
        "flex items-center rounded-md has-aria-invalid:border-destructive has-aria-invalid:ring-[0.1875rem] has-aria-invalid:ring-destructive/20 dark:has-aria-invalid:ring-destructive/40",
        className
      )}
      {...props}
    />
  );
}

function InputOtpSlot({
  index,
  className,
  ...props
}: React.ComponentProps<"div"> & {
  index: number;
}) {
  const inputOtpContext = React.useContext(OTPInputContext);
  const { char, hasFakeCaret, isActive } = inputOtpContext?.slots[index] ?? {};

  return (
    <div
      data-slot="input-otp-slot"
      data-active={isActive}
      // NOTE: This diverges from stock ShadCN to --control-height CSS variable for Apple HIG compliance and use a before pseudo-element for the focus ring (outline-offset gap is transparent and box-shadow gets clipped by adjacent slots; the pseudo-element as a child of the z-10 context renders above adjacent z-0 slots). Transparent left border ensures consistent spacing on all sides.
      className={cn(
        "relative z-0 flex size-[var(--control-height)] items-center justify-center border border-input border-l-transparent text-sm shadow-xs transition-all first:rounded-l-md first:border-l-input last:rounded-r-md aria-invalid:border-destructive data-[active=true]:z-10 data-[active=true]:aria-invalid:border-destructive data-[active=true]:before:pointer-events-none data-[active=true]:before:absolute data-[active=true]:before:-inset-[5px] data-[active=true]:before:z-[-1] data-[active=true]:before:rounded-[inherit] data-[active=true]:before:border-2 data-[active=true]:before:border-ring data-[active=true]:before:bg-background dark:bg-input/30",
        className
      )}
      {...props}
    >
      {char}
      {hasFakeCaret && (
        <div className="pointer-events-none absolute inset-0 flex items-center justify-center">
          <div className="h-4 w-px animate-caret-blink bg-foreground duration-1000" />
        </div>
      )}
    </div>
  );
}

function InputOtpSeparator({ ...props }: React.ComponentProps<"div">) {
  return (
    <div data-slot="input-otp-separator" className="flex items-center [&_svg:not([class*='size-'])]:size-4" {...props}>
      <MinusIcon aria-hidden="true" />
    </div>
  );
}

export { InputOtp, InputOtpGroup, InputOtpSlot, InputOtpSeparator };
