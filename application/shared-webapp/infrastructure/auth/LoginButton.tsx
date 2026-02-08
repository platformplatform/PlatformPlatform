import { Button } from "@repo/ui/components/Button";
import { useNavigate } from "@tanstack/react-router";
import type { ComponentPropsWithoutRef } from "react";
import { loginPath } from "./constants";

type LoginButtonProps = {
  customLoginPath?: string;
} & Omit<ComponentPropsWithoutRef<typeof Button>, "onClick">;

export function LoginButton({ customLoginPath, children, ...props }: LoginButtonProps) {
  const navigate = useNavigate();
  return (
    <Button {...props} onClick={() => navigate({ to: customLoginPath ?? loginPath })}>
      {children}
    </Button>
  );
}
