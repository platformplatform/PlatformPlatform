import { Trans } from "@lingui/react/macro";
import {
  NavigationMenu,
  NavigationMenuContent,
  NavigationMenuItem,
  NavigationMenuLink,
  NavigationMenuList,
  NavigationMenuTrigger
} from "@repo/ui/components/NavigationMenu";
import {
  BookmarkIcon,
  BookOpenIcon,
  FlameIcon,
  GlobeIcon,
  GraduationCapIcon,
  MapIcon,
  SoupIcon,
  UtensilsIcon
} from "lucide-react";

export function NavigationMenuPreview() {
  return (
    <section className="flex flex-col gap-3">
      <h3>
        <Trans>NavigationMenu</Trans>
      </h3>
      <p className="text-sm text-muted-foreground">
        <Trans>
          Horizontal nav bar with rich dropdown sections. Use it for marketing-style site headers or top-level discovery
          navigation.
        </Trans>
      </p>
      <NavigationMenu>
        <NavigationMenuList>
          <NavigationMenuItem>
            <NavigationMenuTrigger>
              <Trans>Recipes</Trans>
            </NavigationMenuTrigger>
            <NavigationMenuContent>
              <ul className="grid w-[28rem] gap-2 p-2 md:grid-cols-2">
                <NavigationMenuLink href="#" className="flex-col items-start gap-1">
                  <span className="flex items-center gap-2 font-medium">
                    <UtensilsIcon />
                    <Trans>Quick weeknight meals</Trans>
                  </span>
                  <span className="text-sm text-muted-foreground">
                    <Trans>Under 30 minutes</Trans>
                  </span>
                </NavigationMenuLink>
                <NavigationMenuLink href="#" className="flex-col items-start gap-1">
                  <span className="flex items-center gap-2 font-medium">
                    <FlameIcon />
                    <Trans>Slow-cooked classics</Trans>
                  </span>
                  <span className="text-sm text-muted-foreground">
                    <Trans>Boeuf, ragù, biryani</Trans>
                  </span>
                </NavigationMenuLink>
                <NavigationMenuLink href="#" className="flex-col items-start gap-1">
                  <span className="flex items-center gap-2 font-medium">
                    <SoupIcon />
                    <Trans>Soups and stews</Trans>
                  </span>
                  <span className="text-sm text-muted-foreground">
                    <Trans>Comfort in a bowl</Trans>
                  </span>
                </NavigationMenuLink>
                <NavigationMenuLink href="#" className="flex-col items-start gap-1">
                  <span className="flex items-center gap-2 font-medium">
                    <BookOpenIcon />
                    <Trans>Browse all recipes</Trans>
                  </span>
                  <span className="text-sm text-muted-foreground">
                    <Trans>Full library</Trans>
                  </span>
                </NavigationMenuLink>
              </ul>
            </NavigationMenuContent>
          </NavigationMenuItem>
          <NavigationMenuItem>
            <NavigationMenuTrigger>
              <Trans>Discover</Trans>
            </NavigationMenuTrigger>
            <NavigationMenuContent>
              <ul className="grid w-[28rem] gap-2 p-2 md:grid-cols-2">
                <NavigationMenuLink href="#" className="flex-col items-start gap-1">
                  <span className="flex items-center gap-2 font-medium">
                    <GlobeIcon />
                    <Trans>Cuisines of the world</Trans>
                  </span>
                  <span className="text-sm text-muted-foreground">
                    <Trans>Italian, Thai, Mexican and more</Trans>
                  </span>
                </NavigationMenuLink>
                <NavigationMenuLink href="#" className="flex-col items-start gap-1">
                  <span className="flex items-center gap-2 font-medium">
                    <BookmarkIcon />
                    <Trans>Editor's picks</Trans>
                  </span>
                  <span className="text-sm text-muted-foreground">
                    <Trans>Curated weekly</Trans>
                  </span>
                </NavigationMenuLink>
                <NavigationMenuLink href="#" className="flex-col items-start gap-1">
                  <span className="flex items-center gap-2 font-medium">
                    <GraduationCapIcon />
                    <Trans>Cooking techniques</Trans>
                  </span>
                  <span className="text-sm text-muted-foreground">
                    <Trans>Knife skills, sauces, fermentation</Trans>
                  </span>
                </NavigationMenuLink>
                <NavigationMenuLink href="#" className="flex-col items-start gap-1">
                  <span className="flex items-center gap-2 font-medium">
                    <MapIcon />
                    <Trans>Local seasonal</Trans>
                  </span>
                  <span className="text-sm text-muted-foreground">
                    <Trans>What's at the market this week</Trans>
                  </span>
                </NavigationMenuLink>
              </ul>
            </NavigationMenuContent>
          </NavigationMenuItem>
          <NavigationMenuItem>
            <NavigationMenuLink href="#">
              <Trans>Cookbooks</Trans>
            </NavigationMenuLink>
          </NavigationMenuItem>
          <NavigationMenuItem>
            <NavigationMenuLink href="#">
              <Trans>Pricing</Trans>
            </NavigationMenuLink>
          </NavigationMenuItem>
        </NavigationMenuList>
      </NavigationMenu>
    </section>
  );
}
