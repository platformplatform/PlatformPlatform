import { plural } from "@lingui/core/macro";
import { Avatar, AvatarFallback, AvatarImage } from "@repo/ui/components/Avatar";
import { Badge } from "@repo/ui/components/Badge";
import { TenantLogo } from "@repo/ui/components/TenantLogo";
import { Link } from "@tanstack/react-router";
import { MailIcon } from "lucide-react";

import type { Schemas, UserRole } from "@/shared/lib/api/client";

import { getSubscriptionPlanLabel, getUserRoleLabel } from "@/shared/lib/api/labels";
import { getSubscriptionPlanBadgeClass } from "@/shared/lib/planBadge";

import { TenantStatusBadge } from "../../accounts/-components/TenantStatusBadge";
import { getInitials } from "./displayName";

// Borderless row that matches the Owners list in /accounts/$tenantId — same hover/active accent, no
// card chrome, no chevron — so the side pane reads as a continuation of the section, not a card grid.
const rowClass =
  "-mx-2 flex w-full cursor-pointer items-center gap-3 rounded-md px-2 py-1 text-left outline-ring transition-colors hover:bg-accent focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 active:bg-accent";

export function ReporterRow({ reporter }: { reporter: Schemas["StaffTicketReporter"] }) {
  const displayName = `${reporter.firstName ?? ""} ${reporter.lastName ?? ""}`.trim() || reporter.email;
  return (
    <Link to="/users/$userId" params={{ userId: reporter.id }} className={rowClass}>
      <Avatar size="default" className="size-10">
        <AvatarImage src={reporter.avatarUrl ?? undefined} alt="" />
        <AvatarFallback>{getInitials(displayName)}</AvatarFallback>
      </Avatar>
      <div className="flex min-w-0 flex-1 flex-col gap-1">
        <span className="truncate text-sm font-medium">{displayName}</span>
        <div className="flex flex-wrap items-center gap-1.5">
          <Badge variant="outline" className="text-[0.6875rem]">
            {getUserRoleLabel(reporter.roleSnapshot as UserRole)}
          </Badge>
          <span className="text-xs text-muted-foreground">
            {plural(reporter.tenantTicketCount, { one: "# ticket", other: "# tickets" })}
          </span>
        </div>
        <span className="flex items-center gap-1 truncate text-xs text-muted-foreground">
          <MailIcon className="size-3 shrink-0" aria-hidden={true} />
          <span className="truncate">{reporter.email}</span>
        </span>
      </div>
    </Link>
  );
}

export function AccountRow({ account }: { account: Schemas["StaffTicketAccount"] }) {
  return (
    <Link to="/accounts/$tenantId" params={{ tenantId: account.id }} className={rowClass}>
      <TenantLogo logoUrl={account.logoUrl} tenantName={account.name} size="md" className="size-10" />
      <div className="flex min-w-0 flex-1 flex-col gap-1">
        <span className="truncate text-sm font-medium">{account.name}</span>
        <div className="flex flex-wrap items-center gap-1.5">
          <Badge className={getSubscriptionPlanBadgeClass(account.plan)}>
            {getSubscriptionPlanLabel(account.plan)}
          </Badge>
          <TenantStatusBadge
            plan={account.plan}
            plannedChange={account.plannedChange}
            hasEverSubscribed={account.hasEverSubscribed}
          />
        </div>
      </div>
    </Link>
  );
}
