import { t } from "@lingui/core/macro";
import { Button } from "@repo/ui/components/Button";
import { Tooltip, TooltipTrigger } from "@repo/ui/components/Tooltip";
import { MailQuestion } from "lucide-react";
import { SupportDialog } from "./SupportDialog";
import "@repo/ui/tailwind.css";

export default function SupportButton() {
  return (
    <SupportDialog>
      <TooltipTrigger>
        <Button variant="icon" aria-label={t`Contact support`}>
          <MailQuestion size={20} />
        </Button>
        <Tooltip>{t`Contact support`}</Tooltip>
      </TooltipTrigger>
    </SupportDialog>
  );
}
