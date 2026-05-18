import { useEffect, useState } from "react";

import { AlertsBadgesPreview } from "./AlertsBadgesPreview";
import { ButtonsPreview } from "./ButtonsPreview";
import { ControlsPreview } from "./ControlsPreview";
import { MediaTab } from "./MediaTab";
import { NavigationPreview } from "./NavigationPreview";
import { OverlaysPreview } from "./OverlaysPreview";
import { ResizablePreview } from "./ResizablePreview";
import { SidebarPreview } from "./SidebarPreview";
import { TabsPreview } from "./TabsPreview";

const DEFAULT_SECTION = "controls";

const sections: Record<string, React.ReactNode> = {
  controls: <ControlsPreview />,
  buttons: <ButtonsPreview />,
  alerts: <AlertsBadgesPreview />,
  navigation: <NavigationPreview />,
  overlays: <OverlaysPreview />,
  resizable: <ResizablePreview />,
  sidebar: <SidebarPreview />,
  tabs: <TabsPreview />,
  media: <MediaTab />
};

export function ComponentPreview() {
  const [activeSection, setActiveSection] = useState(() => window.location.hash.replace("#", "") || DEFAULT_SECTION);

  useEffect(() => {
    const handleHashChange = () => setActiveSection(window.location.hash.replace("#", "") || DEFAULT_SECTION);
    window.addEventListener("hashchange", handleHashChange);
    return () => window.removeEventListener("hashchange", handleHashChange);
  }, []);

  return <div className="flex flex-1 flex-col">{sections[activeSection] ?? sections[DEFAULT_SECTION]}</div>;
}
