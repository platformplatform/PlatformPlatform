import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { Avatar, AvatarFallback, AvatarImage } from "@repo/ui/components/Avatar";
import { Badge } from "@repo/ui/components/Badge";
import { Button } from "@repo/ui/components/Button";
import {
  Item,
  ItemActions,
  ItemContent,
  ItemDescription,
  ItemGroup,
  ItemMedia,
  ItemSeparator,
  ItemTitle
} from "@repo/ui/components/Item";
import { BadgeCheckIcon, BellIcon, ChevronRightIcon, KeyIcon, ShieldIcon } from "lucide-react";

export function ItemPreview() {
  return (
    <div className="flex flex-col gap-6">
      <ItemVariantsSection />
      <ItemAsButtonSection />
      <ItemGroupSection />
      <ItemImageSection />
    </div>
  );
}

function ItemVariantsSection() {
  return (
    <div className="flex flex-col gap-2">
      <h4>
        <Trans>Item — variants</Trans>
      </h4>
      <div className="flex max-w-md flex-col gap-3">
        <Item variant="outline">
          <ItemContent>
            <ItemTitle>
              <Trans>Outline</Trans>
            </ItemTitle>
            <ItemDescription>
              <Trans>Bordered row for stand-alone settings or list items.</Trans>
            </ItemDescription>
          </ItemContent>
          <ItemActions>
            <Button variant="outline" size="sm">
              <Trans>Action</Trans>
            </Button>
          </ItemActions>
        </Item>
        <Item variant="muted">
          <ItemContent>
            <ItemTitle>
              <Trans>Muted</Trans>
            </ItemTitle>
            <ItemDescription>
              <Trans>Subtle background for grouped rows inside a panel.</Trans>
            </ItemDescription>
          </ItemContent>
          <ItemActions>
            <Button variant="outline" size="sm">
              <Trans>Action</Trans>
            </Button>
          </ItemActions>
        </Item>
      </div>
    </div>
  );
}

function ItemAsButtonSection() {
  return (
    <div className="flex flex-col gap-2">
      <h4>
        <Trans>Item — clickable row</Trans>
      </h4>
      <div className="max-w-md">
        <Item variant="outline" size="sm" render={<button type="button" aria-label={t`View profile verification`} />}>
          <ItemMedia>
            <BadgeCheckIcon className="size-5" />
          </ItemMedia>
          <ItemContent>
            <ItemTitle>
              <Trans>Your profile has been verified</Trans>
            </ItemTitle>
          </ItemContent>
          <ItemActions>
            <ChevronRightIcon className="size-4" />
          </ItemActions>
        </Item>
      </div>
    </div>
  );
}

function ItemGroupSection() {
  return (
    <div className="flex flex-col gap-2">
      <h4>
        <Trans>ItemGroup — settings list</Trans>
      </h4>
      <div className="max-w-md rounded-md border border-border bg-card p-2">
        <ItemGroup>
          <Item>
            <ItemMedia variant="icon">
              <ShieldIcon />
            </ItemMedia>
            <ItemContent>
              <ItemTitle>
                <Trans>Two-factor authentication</Trans>
              </ItemTitle>
              <ItemDescription>
                <Trans>Add an extra layer of security to your account.</Trans>
              </ItemDescription>
            </ItemContent>
            <ItemActions>
              <Badge variant="outline">
                <Trans>On</Trans>
              </Badge>
            </ItemActions>
          </Item>
          <ItemSeparator />
          <Item>
            <ItemMedia variant="icon">
              <KeyIcon />
            </ItemMedia>
            <ItemContent>
              <ItemTitle>
                <Trans>Passkeys</Trans>
              </ItemTitle>
              <ItemDescription>
                <Trans>Sign in without a password using your device.</Trans>
              </ItemDescription>
            </ItemContent>
            <ItemActions>
              <Button variant="outline" size="sm">
                <Trans>Manage</Trans>
              </Button>
            </ItemActions>
          </Item>
          <ItemSeparator />
          <Item>
            <ItemMedia variant="icon">
              <BellIcon />
            </ItemMedia>
            <ItemContent>
              <ItemTitle>
                <Trans>Login alerts</Trans>
              </ItemTitle>
              <ItemDescription>
                <Trans>Email me when a new device signs in.</Trans>
              </ItemDescription>
            </ItemContent>
            <ItemActions>
              <Badge variant="outline">
                <Trans>Off</Trans>
              </Badge>
            </ItemActions>
          </Item>
        </ItemGroup>
      </div>
    </div>
  );
}

function ItemImageSection() {
  return (
    <div className="flex flex-col gap-2">
      <h4>
        <Trans>Item — image media</Trans>
      </h4>
      <div className="max-w-md">
        <Item variant="outline">
          <ItemMedia variant="image">
            <Avatar>
              <AvatarImage src="https://i.pravatar.cc/80?img=12" alt="Sarah Carter" />
              <AvatarFallback>SC</AvatarFallback>
            </Avatar>
          </ItemMedia>
          <ItemContent>
            <ItemTitle>Sarah Carter</ItemTitle>
            <ItemDescription>sarah.carter@example.com</ItemDescription>
          </ItemContent>
          <ItemActions>
            <Button variant="outline" size="sm">
              <Trans>Invite</Trans>
            </Button>
          </ItemActions>
        </Item>
      </div>
    </div>
  );
}
