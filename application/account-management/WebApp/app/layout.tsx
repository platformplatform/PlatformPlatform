import { useNavigate } from "react-router-dom";
import { Trans } from "@lingui/macro";
import AcmeLogo from "@/ui/acme-logo.svg";
import { LocaleSwitcher } from "@/ui/LocaleSwitcher";
import { Button } from "@/ui/components/Button";

interface LayoutProps {
  children: React.ReactNode;
  params: Record<string, string>;
}

export default function Root({ children }: Readonly<LayoutProps>) {
  const navigate = useNavigate();

  function handleCreateTenant() {
    navigate("/tenant/create");
  }

  return (
    <div className="flex flex-row h-full w-full">
      <div className="flex gap-2 flex-col h-full w-80 border-r border-border bg-gray-100 px-6">
        <h1 className="flex gap-1 items-center order-1 border-t border-border px-4 py-8">
          <AcmeLogo className="w-6 h-6" />
          {" "}
          ACME Company
        </h1>
        <div className="justify-start flex flex-row border-b border-border py-4">
          <Button variant="secondary" className="w-full" onPress={handleCreateTenant}>
            <Trans>Create Account</Trans>
          </Button>
        </div>
        <nav className="grow">
          <ul>
            <li className="p-4 hover:bg-gray-200 rounded-xl cursor-pointer">
              <a href="/">
                <Trans>Account Management</Trans>
              </a>
            </li>
            <li className="p-4 hover:bg-gray-200 rounded-xl cursor-pointer">
              <a href="/user-management">
                <Trans>User Management</Trans>
              </a>
            </li>
          </ul>
        </nav>
        <LocaleSwitcher />
      </div>
      <div className="flex flex-col w-full h-full bg-background">{children}</div>
    </div>
  );
}
