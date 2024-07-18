import type { PropsWithChildren } from "react";
import { tv } from "tailwind-variants";

const cardStyles = tv({
  base: "flex flex-col gap-2 bg-card text-card-foreground border-2 border-border rounded-lg shadow-sm p-4 w-fit items-center justify-center"
});

type CardProps = {
  className?: string;
} & PropsWithChildren;

export function Card({ className, children }: CardProps) {
  return <div className={cardStyles({ className })}>{children}</div>;
}
