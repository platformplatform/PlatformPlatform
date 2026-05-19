import { Trans } from "@lingui/react/macro";
import { Button } from "@repo/ui/components/Button";
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from "@repo/ui/components/Card";
import { PlusIcon } from "lucide-react";

export function CardsPreview() {
  return (
    <div className="flex flex-col gap-2">
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
        <Card>
          <CardHeader>
            <CardTitle>
              <Trans>Saved recipes</Trans>
            </CardTitle>
            <CardDescription>
              <Trans>Your personal cookbook library.</Trans>
            </CardDescription>
          </CardHeader>
          <CardContent>
            <p>
              <Trans>You have 12 saved recipes.</Trans>
            </p>
          </CardContent>
          <CardFooter>
            <Button variant="outline" size="sm">
              <Trans>View all</Trans>
            </Button>
          </CardFooter>
        </Card>
        <Card>
          <CardHeader>
            <CardTitle>
              <Trans>Co-cooks</Trans>
            </CardTitle>
            <CardDescription>
              <Trans>Share recipes with family and friends.</Trans>
            </CardDescription>
          </CardHeader>
          <CardContent>
            <p>
              <Trans>5 active co-cooks</Trans>
            </p>
          </CardContent>
          <CardFooter>
            <Button size="sm">
              <PlusIcon />
              <Trans>Invite</Trans>
            </Button>
          </CardFooter>
        </Card>
        <Card>
          <CardHeader>
            <CardTitle>
              <Trans>Cooking time</Trans>
            </CardTitle>
            <CardDescription>
              <Trans>Total time spent cooking this month.</Trans>
            </CardDescription>
          </CardHeader>
          <CardContent>
            <p>
              <Trans>14 h 30 min logged</Trans>
            </p>
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
