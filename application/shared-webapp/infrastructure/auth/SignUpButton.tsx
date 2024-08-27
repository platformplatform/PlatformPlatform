import { Button, type ButtonProps } from "@repo/ui/components/Button";
import { useNavigate } from "@tanstack/react-router";
import { signUpPath } from "./constants";

type SignUpButtonProps = {
  customSignUpPath?: string;
} & Omit<ButtonProps, "onPress">;

export function SignUpButton({ customSignUpPath, children, ...props }: SignUpButtonProps) {
  const navigate = useNavigate();
  return (
    <Button {...props} onPress={() => navigate({ to: customSignUpPath ?? signUpPath })}>
      {children}
    </Button>
  );
}
