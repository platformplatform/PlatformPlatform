import { useLingui } from "@lingui/react";
import {
  BugIcon,
  CreditCardIcon,
  HelpCircleIcon,
  type LucideIcon,
  MessageSquareHeartIcon,
  MoreHorizontalIcon,
  SparklesIcon,
  UsersIcon
} from "lucide-react";

import { SupportTicketCategory } from "@/shared/lib/api/client";

import { categoryLabels } from "./statusMaps";

const categoryIcons: Record<SupportTicketCategory, LucideIcon> = {
  [SupportTicketCategory.Billing]: CreditCardIcon,
  [SupportTicketCategory.Account]: UsersIcon,
  [SupportTicketCategory.HowTo]: HelpCircleIcon,
  [SupportTicketCategory.Bug]: BugIcon,
  [SupportTicketCategory.Feature]: SparklesIcon,
  [SupportTicketCategory.Feedback]: MessageSquareHeartIcon,
  [SupportTicketCategory.Other]: MoreHorizontalIcon
};

export function CategoryPill({ category }: { category: SupportTicketCategory }) {
  const { i18n } = useLingui();
  const Icon = categoryIcons[category];
  return (
    <span className="inline-flex items-center gap-1 rounded-full bg-muted px-2 py-0.5 text-xs font-medium text-muted-foreground">
      <Icon className="size-3" aria-hidden={true} />
      {i18n._(categoryLabels[category])}
    </span>
  );
}

export { categoryIcons };
