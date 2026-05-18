import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Badge } from "@repo/ui/components/Badge";
import { Button } from "@repo/ui/components/Button";
import {
  Item,
  ItemActions,
  ItemContent,
  ItemDescription,
  ItemGroup,
  ItemMedia,
  ItemTitle
} from "@repo/ui/components/Item";
import { SidePane, SidePaneBody, SidePaneFooter, SidePaneHeader } from "@repo/ui/components/SidePane";
import { useFormatDate } from "@repo/ui/hooks/useSmartDate";
import { PencilIcon, Trash2Icon } from "lucide-react";

import type { SampleDish } from "./sampleDishData";

import { difficultyVariant, formatCookTime } from "./sampleDishData";

const cuisineColor: Record<SampleDish["cuisine"], string> = {
  Italian: "bg-rose-500/20 text-rose-600 dark:text-rose-400",
  Japanese: "bg-blue-500/20 text-blue-600 dark:text-blue-400",
  Mexican: "bg-amber-500/20 text-amber-600 dark:text-amber-400",
  French: "bg-purple-500/20 text-purple-600 dark:text-purple-400",
  Indian: "bg-orange-500/20 text-orange-600 dark:text-orange-400",
  Thai: "bg-green-500/20 text-green-600 dark:text-green-400"
};

interface DishDetailsSidePaneProps {
  dish: SampleDish | null;
  isOpen: boolean;
  onClose: () => void;
}

export function DishDetailsSidePane({ dish, isOpen, onClose }: DishDetailsSidePaneProps) {
  const formatDate = useFormatDate();

  if (!dish) {
    return null;
  }

  return (
    <SidePane
      isOpen={isOpen}
      onOpenChange={(open) => !open && onClose()}
      trackingTitle="Recipe details"
      trackingKey={String(dish.id)}
      aria-label={t`Recipe details`}
    >
      <SidePaneHeader closeButtonLabel={t`Close recipe details`}>
        <Trans>Recipe details</Trans>
      </SidePaneHeader>

      <SidePaneBody className="flex flex-col gap-6">
        <Item className="p-0">
          <ItemMedia className={`size-12 rounded-lg text-lg font-semibold ${cuisineColor[dish.cuisine]}`}>
            {dish.name[0]}
          </ItemMedia>
          <ItemContent>
            <ItemTitle className="truncate font-semibold">{dish.name}</ItemTitle>
            <ItemDescription>{dish.description}</ItemDescription>
          </ItemContent>
        </Item>

        <div className="grid grid-cols-2 gap-3">
          <div className="rounded-lg border border-border bg-muted/40 p-3">
            <p className="text-xs text-muted-foreground">
              <Trans>Cook time</Trans>
            </p>
            <p className="mt-1 text-lg font-semibold">{formatCookTime(dish.cookTime)}</p>
          </div>
          <div className="rounded-lg border border-border bg-muted/40 p-3">
            <p className="text-xs text-muted-foreground">
              <Trans>Difficulty</Trans>
            </p>
            <div className="mt-1">
              <Badge variant={difficultyVariant[dish.difficulty]}>{dish.difficulty}</Badge>
            </div>
          </div>
        </div>

        <ItemGroup>
          <Item size="xs">
            <ItemContent>
              <ItemTitle className="text-sm font-normal text-muted-foreground">
                <Trans>Cuisine</Trans>
              </ItemTitle>
            </ItemContent>
            <ItemActions>
              <span className="text-sm font-medium">{dish.cuisine}</span>
            </ItemActions>
          </Item>
          <Item size="xs">
            <ItemContent>
              <ItemTitle className="text-sm font-normal text-muted-foreground">
                <Trans>Added</Trans>
              </ItemTitle>
            </ItemContent>
            <ItemActions>
              <span className="text-sm font-medium">{formatDate(dish.addedAt)}</span>
            </ItemActions>
          </Item>
          <Item size="xs">
            <ItemContent>
              <ItemTitle className="text-sm font-normal text-muted-foreground">
                <Trans>Recipe ID</Trans>
              </ItemTitle>
            </ItemContent>
            <ItemActions>
              <span className="font-mono text-sm text-muted-foreground">#{dish.id}</span>
            </ItemActions>
          </Item>
        </ItemGroup>
      </SidePaneBody>

      <SidePaneFooter className="flex flex-col gap-2">
        <Button className="w-full">
          <PencilIcon />
          <Trans>Edit recipe</Trans>
        </Button>
        <Button variant="destructive" className="w-full" aria-label={t`Delete recipe`}>
          <Trash2Icon />
          <Trans>Delete recipe</Trans>
        </Button>
      </SidePaneFooter>
    </SidePane>
  );
}
