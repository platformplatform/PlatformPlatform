import { Trans } from "@lingui/react/macro";
import { Accordion, AccordionContent, AccordionItem, AccordionTrigger } from "@repo/ui/components/Accordion";
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from "@repo/ui/components/Collapsible";
import { cn } from "@repo/ui/utils";
import { ChevronDownIcon } from "lucide-react";
import { useState } from "react";

export function AccordionPreview() {
  const [collapsibleOpen, setCollapsibleOpen] = useState(false);

  return (
    <div className="flex flex-col gap-8 md:flex-row md:gap-12">
      <section className="flex-1">
        <h3 className="mb-3">
          <Trans>Accordion</Trans>
        </h3>
        <Accordion defaultValue={["0"]}>
          <AccordionItem value="0">
            <AccordionTrigger>
              <Trans>When to use Accordion</Trans>
            </AccordionTrigger>
            <AccordionContent>
              <p>
                <Trans>
                  Use Accordion to stack multiple related disclosable sections. Good for FAQs, settings groups, or any
                  list where users scan items and expand a few.
                </Trans>
              </p>
            </AccordionContent>
          </AccordionItem>
          <AccordionItem value="1">
            <AccordionTrigger>
              <Trans>Single-expand vs multi-expand</Trans>
            </AccordionTrigger>
            <AccordionContent>
              <p>
                <Trans>
                  By default only one item is open at a time. Pass openMultiple to allow several panels to stay open
                  together.
                </Trans>
              </p>
            </AccordionContent>
          </AccordionItem>
          <AccordionItem value="2">
            <AccordionTrigger>
              <Trans>Accordion vs Collapsible</Trans>
            </AccordionTrigger>
            <AccordionContent>
              <p>
                <Trans>
                  Reach for Collapsible when you only need one trigger and one panel. Use Accordion when several items
                  share visual rhythm and behavior.
                </Trans>
              </p>
            </AccordionContent>
          </AccordionItem>
        </Accordion>
      </section>
      <section className="flex-1">
        <h3 className="mb-3">
          <Trans>Collapsible</Trans>
        </h3>
        <Collapsible open={collapsibleOpen} onOpenChange={setCollapsibleOpen}>
          <CollapsibleTrigger className="flex w-full cursor-pointer items-center justify-between rounded-md border border-input bg-white px-4 py-3 text-sm font-medium outline-ring transition-colors hover:bg-accent focus-visible:outline-2 focus-visible:outline-offset-2 active:bg-muted/50 dark:bg-input/30">
            <Trans>Advanced options</Trans>
            <ChevronDownIcon
              className={cn(
                "size-4 text-muted-foreground transition-transform duration-200",
                collapsibleOpen && "rotate-180"
              )}
            />
          </CollapsibleTrigger>
          <CollapsibleContent className="overflow-hidden data-closed:animate-accordion-up data-open:animate-accordion-down">
            <div className="mt-2 rounded-md border border-input bg-card p-4 text-sm">
              <p>
                <Trans>
                  Collapsible is the unstyled single-section primitive behind Accordion. Use it when you need one
                  trigger and one panel.
                </Trans>
              </p>
            </div>
          </CollapsibleContent>
        </Collapsible>
      </section>
    </div>
  );
}
