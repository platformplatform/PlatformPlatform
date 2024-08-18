import { Button, type ButtonProps } from "@repo/ui/components/Button";
import { useNavigate } from "@tanstack/react-router";
import { loginPath } from "./constants";

type LoginButtonProps = {
  customLoginPath?: string;
} & Omit<ButtonProps, "onPress">;

export function LoginButton({ customLoginPath, children, ...props }: LoginButtonProps) {
  const navigate = useNavigate();
  return (
    <Button {...props} onPress={() => navigate({ to: customLoginPath ?? loginPath })}>
      {children}
    </Button>
  );
}
