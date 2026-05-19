import type { TableRowSize } from "@repo/ui/components/Table";

import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Badge } from "@repo/ui/components/Badge";
import { Button } from "@repo/ui/components/Button";
import { Checkbox } from "@repo/ui/components/Checkbox";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger
} from "@repo/ui/components/DropdownMenu";
import { TableCell, TableRow } from "@repo/ui/components/Table";
import { CopyIcon, EllipsisVerticalIcon, PencilIcon, Trash2Icon } from "lucide-react";

import type { SampleDish } from "./sampleDishData";

import { difficultyVariant, formatCookTime } from "./sampleDishData";

interface DishRowProps {
  dish: SampleDish;
  rowSize: TableRowSize;
  formatDate: (date: string) => string;
  showCheckbox?: boolean;
  isChecked?: boolean;
}

export function DishRow({ dish, rowSize, formatDate, showCheckbox, isChecked }: Readonly<DishRowProps>) {
  return (
    <TableRow rowKey={dish.id}>
      {showCheckbox && (
        <TableCell>
          <Checkbox
            checked={isChecked ?? false}
            onCheckedChange={() => {
              /* selection is driven by the Table click delegation */
            }}
            aria-label={t`Select ${dish.name}`}
          />
        </TableCell>
      )}
      <TableCell>
        {rowSize === "spacious" ? (
          <div className="flex flex-col">
            <span className="font-medium">{dish.name}</span>
            <span className="text-sm text-muted-foreground">{dish.description}</span>
          </div>
        ) : (
          <span className="font-medium">{dish.name}</span>
        )}
      </TableCell>
      <TableCell>{dish.cuisine}</TableCell>
      <TableCell>{formatCookTime(dish.cookTime)}</TableCell>
      <TableCell>{formatDate(dish.addedAt)}</TableCell>
      <TableCell>
        <Badge variant={difficultyVariant[dish.difficulty]}>{dish.difficulty}</Badge>
      </TableCell>
      <TableCell>
        <DropdownMenu trackingTitle="Recipe actions">
          <DropdownMenuTrigger
            render={
              <Button variant="ghost" size="icon" aria-label={t`Recipe actions`}>
                <EllipsisVerticalIcon className="size-5 text-muted-foreground" />
              </Button>
            }
          />
          <DropdownMenuContent>
            <DropdownMenuItem>
              <PencilIcon className="size-4" />
              <Trans>Edit recipe</Trans>
            </DropdownMenuItem>
            <DropdownMenuItem>
              <CopyIcon className="size-4" />
              <Trans>Duplicate</Trans>
            </DropdownMenuItem>
            <DropdownMenuSeparator />
            <DropdownMenuItem variant="destructive">
              <Trash2Icon className="size-4" />
              <Trans>Delete</Trans>
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      </TableCell>
    </TableRow>
  );
}
