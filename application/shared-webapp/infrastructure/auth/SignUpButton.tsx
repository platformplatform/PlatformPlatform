import type { ComponentPropsWithoutRef } from "react";

import { Button } from "@repo/ui/components/Button";
import { useNavigate } from "@tanstack/react-router";

import { signUpPath } from "./constants";

type SignUpButtonProps = {
  customSignUpPath?: string;
} & Omit<ComponentPropsWithoutRef<typeof Button>, "onClick">;

export function SignUpButton({ customSignUpPath, children, ...props }: SignUpButtonProps) {
  const navigate = useNavigate();
  return (
    <Button {...props} onClick={() => navigate({ to: customSignUpPath ?? signUpPath })}>
      {children}
    </Button>
  );
}
