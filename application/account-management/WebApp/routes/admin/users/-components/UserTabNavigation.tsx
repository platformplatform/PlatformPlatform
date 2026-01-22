import { Trans } from "@lingui/react/macro";
import { Link } from "@tanstack/react-router";

type UserTabNavigationProps = {
  activeTab: "all-users" | "recycle-bin";
};

export function UserTabNavigation({ activeTab }: UserTabNavigationProps) {
  const baseTabClasses =
    "relative flex cursor-pointer items-center gap-2 rounded-md px-4 py-2 text-center font-semibold text-sm outline-ring transition-colors focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 after:absolute after:right-1 after:-bottom-px after:left-1 after:h-0.5 after:transition-colors";
  const selectedClasses = "text-foreground after:bg-primary";
  const unselectedClasses = "text-muted-foreground hover:text-muted-foreground/90 after:bg-transparent";

  return (
    <nav className="mb-8 flex gap-4 border-border border-b" aria-label="User tabs">
      <Link
        to="/admin/users"
        className={`${baseTabClasses} ${activeTab === "all-users" ? selectedClasses : unselectedClasses}`}
      >
        <Trans>All users</Trans>
      </Link>
      <Link
        to="/admin/users/recycle-bin"
        className={`${baseTabClasses} ${activeTab === "recycle-bin" ? selectedClasses : unselectedClasses}`}
      >
        <Trans>Recycle bin</Trans>
      </Link>
    </nav>
  );
}
