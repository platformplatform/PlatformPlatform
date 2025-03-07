/**
 * ref: https://react-spectrum.adobe.com/react-spectrum/IllustratedMessage.html
 */
import type { PropsWithChildren } from "react";

type IllustratedMessageProps = PropsWithChildren;

export function IllustratedMessage({ children }: Readonly<IllustratedMessageProps>) {
  return (
    <div className="flex flex-col items-center justify-center gap-2 stroke-muted-foreground [&>svg]:mb-4">
      {children}
    </div>
  );
}

export function Heading({ children }: Readonly<{ slot?: string } & PropsWithChildren>) {
  return <h1 className="max-w-md text-center font-semibold text-foreground text-xl">{children}</h1>;
}

export function Content({ children }: Readonly<PropsWithChildren>) {
  return <section className="max-w-md text-center font-base text-foreground text-sm">{children}</section>;
}
