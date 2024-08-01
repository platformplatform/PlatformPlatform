import { Button, type ButtonProps } from "@repo/ui/components/Button";
import { useNavigate } from "@tanstack/react-router";
import { signInPath } from "./constants";

type SignInButtonProps = {
  customSignInPath?: string;
} & Omit<ButtonProps, "onPress">;

export function SignInButton({ customSignInPath, children, ...props }: SignInButtonProps) {
  const navigate = useNavigate();
  return (
    <Button {...props} onPress={() => navigate({ to: customSignInPath ?? signInPath })}>
      {children}
    </Button>
  );
}
