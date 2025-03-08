/**
 * ref: https://react-spectrum.adobe.com/react-aria-tailwind-starter/?path=/docs/tabs--docs
 * ref: https://ui.shadcn.com/docs/components/tabs
 */
import {
  Tab as AriaTab,
  TabList as AriaTabList,
  TabPanel as AriaTabPanel,
  Tabs as AriaTabs,
  type TabListProps,
  type TabPanelProps,
  type TabProps,
  type TabsProps,
  composeRenderProps
} from "react-aria-components";
import { tv } from "tailwind-variants";
import { focusRing } from "./focusRing";

const tabsStyles = tv({
  base: "flex gap-4",
  variants: {
    orientation: {
      horizontal: "flex-col",
      vertical: "w-[800px] flex-row"
    }
  }
});

export function Tabs(props: Readonly<TabsProps>) {
  return (
    <AriaTabs
      {...props}
      className={composeRenderProps(props.className, (className, renderProps) =>
        tabsStyles({ ...renderProps, className })
      )}
    />
  );
}

const tabListStyles = tv({
  base: "flex gap-1 border-border",
  variants: {
    orientation: {
      horizontal: "flex-row border-b [&>*]:border-b-2",
      vertical: "flex-col items-start border-r [&>*]:w-full [&>*]:border-r-2"
    }
  }
});

export function TabList<T extends object>(props: Readonly<TabListProps<T>>) {
  return (
    <AriaTabList
      {...props}
      className={composeRenderProps(props.className, (className, renderProps) =>
        tabListStyles({ ...renderProps, className })
      )}
    />
  );
}

const tabProps = tv({
  extend: focusRing,
  base: "flex cursor-default items-center gap-2 px-4 pt-1.5 pb-0.5 text-center font-semibold text-sm transition forced-color-adjust-none",
  variants: {
    isSelected: {
      false: "border-transparent text-muted-foreground",
      true: "border-primary text-foreground forced-colors:text-[HighlightText]"
    },
    isHovered: {
      true: "text-muted-foreground/90"
    },
    isFocusVisible: {
      true: "rounded-md border-transparent"
    },
    isDisabled: {
      true: "cursor-not-allowed opacity-50"
    }
  }
});

export function Tab(props: Readonly<TabProps>) {
  return (
    <AriaTab
      {...props}
      className={composeRenderProps(props.className, (className, renderProps) =>
        tabProps({ ...renderProps, className })
      )}
    />
  );
}

const tabPanelStyles = tv({
  extend: focusRing,
  base: "flex-1 rounded-lg border border-border p-4 text-foreground text-sm"
});

export function TabPanel(props: Readonly<TabPanelProps>) {
  return (
    <AriaTabPanel
      {...props}
      className={composeRenderProps(props.className, (className, renderProps) =>
        tabPanelStyles({ ...renderProps, className })
      )}
    />
  );
}
