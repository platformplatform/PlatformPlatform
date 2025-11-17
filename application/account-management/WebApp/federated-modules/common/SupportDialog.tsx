import { t } from "@lingui/core/macro";
import { Button } from "@repo/ui/components/Button";
import { Dialog, DialogTrigger } from "@repo/ui/components/Dialog";
import { DialogContent, DialogFooter, DialogHeader } from "@repo/ui/components/DialogFooter";
import { Heading } from "@repo/ui/components/Heading";
import { Modal } from "@repo/ui/components/Modal";
import { MailIcon, XIcon } from "lucide-react";
import type { ReactNode } from "react";

interface SupportDialogProps {
  children: ReactNode;
}

export function SupportDialog({ children }: Readonly<SupportDialogProps>) {
  return (
    <DialogTrigger>
      {children}
      <Modal isDismissable={true} zIndex="high">
        <Dialog className="sm:w-dialog-md">
          {({ close }) => (
            <>
              <XIcon onClick={close} className="absolute top-2 right-2 h-10 w-10 cursor-pointer p-2 hover:bg-muted" />
              <DialogHeader description={t`Need help? Our support team is here to assist you.`}>
                <Heading slot="title" className="text-2xl">
                  {t`Contact support`}
                </Heading>
              </DialogHeader>
              <DialogContent className="flex flex-col gap-4">
                <div className="flex items-center gap-3 rounded-lg border border-input bg-input-background p-4 opacity-50">
                  <MailIcon className="h-5 w-5 text-muted-foreground" />
                  <a href="mailto:support@platformplatform.net" className="text-primary hover:underline">
                    support@platformplatform.net
                  </a>
                </div>
                <p className="text-muted-foreground text-sm">{t`Feel free to reach out with any questions or issues you may have.`}</p>
              </DialogContent>
              <DialogFooter>
                <Button onPress={close}>{t`Close`}</Button>
              </DialogFooter>
            </>
          )}
        </Dialog>
      </Modal>
    </DialogTrigger>
  );
}
