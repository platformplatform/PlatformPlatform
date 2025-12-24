import { t } from "@lingui/core/macro";
import { Button } from "@repo/ui/components/Button";
import { Tooltip, TooltipContent, TooltipTrigger } from "@repo/ui/components/Tooltip";
import { MailQuestion } from "lucide-react";
import { SupportDialog } from "./SupportDialog";
import "@repo/ui/tailwind.css";

export default function SupportButton() {
  return (
    <SupportDialog>
      <Tooltip>
        <TooltipTrigger
          render={
            <Button variant="ghost" size="icon" aria-label={t`Contact support`}>
              <MailQuestion size={20} />
            </Button>
          }
        />
        <TooltipContent>{t`Contact support`}</TooltipContent>
      </Tooltip>
    </SupportDialog>
  );
}
