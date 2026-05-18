import { t } from "@lingui/core/macro";
import { Plural, Trans } from "@lingui/react/macro";
import { Button } from "@repo/ui/components/Button";
import { Item, ItemActions, ItemContent, ItemGroup, ItemTitle } from "@repo/ui/components/Item";
import { SidePane, SidePaneBody, SidePaneFooter, SidePaneHeader } from "@repo/ui/components/SidePane";
import { Trash2Icon } from "lucide-react";

import type { SampleDish } from "./sampleDishData";

import { formatCookTime } from "./sampleDishData";

interface DishMultiSelectSidePaneProps {
  dishes: SampleDish[];
  totalCount: number;
  isOpen: boolean;
}

export function DishMultiSelectSidePane({ dishes, totalCount, isOpen }: DishMultiSelectSidePaneProps) {
  const totalMinutes = dishes.reduce((sum, dish) => sum + dish.cookTime, 0);

  return (
    <SidePane
      isOpen={isOpen}
      // No-op: the summary pane is not dismissable -- it is driven purely by the current selection
      // state. Esc and backdrop clicks should not close it.
      onOpenChange={() => {}}
      trackingTitle="Recipe selection summary"
      aria-label={t`Selected recipes`}
    >
      <SidePaneHeader showCloseButton={false}>
        <Trans>
          {dishes.length} of {totalCount} recipes selected
        </Trans>
      </SidePaneHeader>

      <SidePaneBody className="flex flex-col gap-4">
        <Item variant="muted" size="sm">
          <ItemContent>
            <ItemTitle className="text-sm font-normal text-muted-foreground">
              <Trans>Total cook time</Trans>
            </ItemTitle>
          </ItemContent>
          <ItemActions>
            <span className="text-lg font-semibold">{formatCookTime(totalMinutes)}</span>
          </ItemActions>
        </Item>

        <ItemGroup>
          {dishes.map((dish) => (
            <Item key={dish.id} size="xs">
              <ItemContent>
                <ItemTitle className="font-medium">{dish.name}</ItemTitle>
              </ItemContent>
              <ItemActions>
                <span className="shrink-0 text-sm text-muted-foreground">{formatCookTime(dish.cookTime)}</span>
              </ItemActions>
            </Item>
          ))}
        </ItemGroup>
      </SidePaneBody>

      <SidePaneFooter>
        <Button variant="destructive" className="w-full" aria-label={t`Delete selected recipes`}>
          <Trash2Icon />
          <Trans>
            <Plural value={dishes.length} one="Delete # recipe" other="Delete # recipes" />
          </Trans>
        </Button>
      </SidePaneFooter>
    </SidePane>
  );
}
