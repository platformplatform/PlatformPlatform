import { AuthenticationProvider } from "@/lib/auth/AuthenticationProvider";
import { useNavigate } from "@/lib/router/router";

interface LayoutProps {
  children: React.ReactNode;
  params: Record<string, string>;
}

export default function Root({ children }: Readonly<LayoutProps>) {
  const navigate = useNavigate();
  return (
    <AuthenticationProvider navigate={navigate} afterSignIn="/dashboard" afterSignOut="/login">
      {children}
    </AuthenticationProvider>
  );
}
