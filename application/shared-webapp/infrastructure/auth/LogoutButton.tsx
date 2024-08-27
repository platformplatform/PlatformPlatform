import { Button, type ButtonProps } from "@repo/ui/components/Button";
import { useNavigate } from "@tanstack/react-router";
import { logoutPath } from "./constants";

type LogoutButtonProps = {
  customLogoutPath?: string;
} & Omit<ButtonProps, "onPress">;

export function LogoutButton({ customLogoutPath, children, ...props }: LogoutButtonProps) {
  const navigate = useNavigate();
  return (
    <Button {...props} onPress={() => navigate({ to: customLogoutPath ?? logoutPath })}>
      {children}
    </Button>
  );
}
