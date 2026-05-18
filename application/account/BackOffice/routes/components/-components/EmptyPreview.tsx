import { Trans } from "@lingui/react/macro";
import { Button } from "@repo/ui/components/Button";
import { Empty, EmptyDescription, EmptyHeader, EmptyMedia, EmptyTitle } from "@repo/ui/components/Empty";
import { ChefHatIcon, PlusIcon } from "lucide-react";

export function EmptyPreview() {
  return (
    <Empty>
      <EmptyHeader>
        <EmptyMedia variant="icon">
          <ChefHatIcon />
        </EmptyMedia>
        <EmptyTitle>
          <Trans>No recipes yet</Trans>
        </EmptyTitle>
        <EmptyDescription>
          <Trans>Add your first recipe to start building your personal cookbook.</Trans>
        </EmptyDescription>
      </EmptyHeader>
      <Button>
        <PlusIcon />
        <Trans>Add recipe</Trans>
      </Button>
    </Empty>
  );
}
