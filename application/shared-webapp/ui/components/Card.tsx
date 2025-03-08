import type { PropsWithChildren } from "react";
import { tv } from "tailwind-variants";

const cardStyles = tv({
  base: "flex w-fit flex-col items-center justify-center gap-2 rounded-lg border-2 border-border bg-card p-4 text-card-foreground shadow-sm"
});

type CardProps = {
  className?: string;
} & PropsWithChildren;

export function Card({ className, children }: CardProps) {
  return <div className={cardStyles({ className })}>{children}</div>;
}
