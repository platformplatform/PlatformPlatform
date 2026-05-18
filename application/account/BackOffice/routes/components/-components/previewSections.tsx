import { Trans } from "@lingui/react/macro";
import {
  BarChart3Icon,
  CompassIcon,
  ImageIcon,
  LayersIcon,
  LayoutDashboardIcon,
  LayoutGridIcon,
  LayoutTemplateIcon,
  type LucideIcon,
  MailIcon,
  MousePointerClickIcon,
  PanelLeftIcon,
  PanelsTopLeftIcon,
  SquareDashedIcon,
  SquareMousePointerIcon,
  TableIcon,
  TagIcon,
  TextCursorInputIcon
} from "lucide-react";

export type PreviewSection = Readonly<{
  hash: string;
  label: React.ReactNode;
  icon: LucideIcon;
}>;

export const componentsSections: readonly PreviewSection[] = [
  { hash: "controls", label: <Trans>Controls</Trans>, icon: TextCursorInputIcon },
  { hash: "buttons", label: <Trans>Buttons and links</Trans>, icon: MousePointerClickIcon },
  { hash: "alerts", label: <Trans>Alerts, badges, and banners</Trans>, icon: TagIcon },
  { hash: "navigation", label: <Trans>Navigation and shortcuts</Trans>, icon: CompassIcon },
  { hash: "overlays", label: <Trans>Overlays</Trans>, icon: LayersIcon },
  { hash: "resizable", label: <Trans>Resizable panels</Trans>, icon: LayoutDashboardIcon },
  { hash: "sidebar", label: <Trans>Sidebar</Trans>, icon: PanelLeftIcon },
  { hash: "tabs", label: <Trans>Tabs</Trans>, icon: SquareMousePointerIcon },
  { hash: "media", label: <Trans>Media</Trans>, icon: ImageIcon }
];

export const examplesSections: readonly PreviewSection[] = [
  { hash: "dialogs", label: <Trans>Dialogs</Trans>, icon: PanelsTopLeftIcon },
  { hash: "cards", label: <Trans>Cards</Trans>, icon: LayoutTemplateIcon },
  { hash: "tables", label: <Trans>Tables and side pane</Trans>, icon: TableIcon },
  { hash: "empty", label: <Trans>Empty</Trans>, icon: LayoutGridIcon },
  { hash: "skeleton", label: <Trans>Skeleton</Trans>, icon: SquareDashedIcon }
];

export const chartsIcon = BarChart3Icon;
export const chartsLabel = <Trans>Charts</Trans>;

export const emailsIcon = MailIcon;
export const emailsLabel = <Trans>Emails</Trans>;

export function findSectionLabel(sections: readonly PreviewSection[], hash: string): React.ReactNode {
  return sections.find((section) => section.hash === hash)?.label ?? sections[0].label;
}
