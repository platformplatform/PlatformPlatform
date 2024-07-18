import { createContext, useContext, useState } from "react";
import { ChevronsLeftIcon, CircleUserIcon, HomeIcon, type LucideIcon, UsersRoundIcon } from "lucide-react";
import { Button } from "./Button";
import { tv } from "tailwind-variants";
const logoWrap = "https://platformplatformgithub.blob.core.windows.net/logo-wrap.svg?url";

const collapsedContext = createContext(false);

const menuButtonStyles = tv({
  base: "flex text-base font-normal w-full justify-start transition-all duration-300",
  variants: {
    isCollapsed: {
      true: "gap-0 ease-out",
      false: "gap-4 ease-in"
    }
  }
});

const menuTextStyles = tv({
  base: "text-foreground transition-all duration-300 text-start",
  variants: {
    isCollapsed: {
      true: "w-0 opacity-0 text-xs ease-out",
      false: "w-fit opacity-100 text-base ease-in"
    }
  }
});

type MenuButtonProps = {
  icon: LucideIcon;
  label: string;
};

function MenuButton({ icon: Icon, label }: Readonly<MenuButtonProps>) {
  const isCollapsed = useContext(collapsedContext);
  return (
    <Button variant="ghost" className={menuButtonStyles({ isCollapsed })}>
      <Icon className="w-6 h-6 shrink-0 grow-0" />
      <div className={menuTextStyles({ isCollapsed })}>{label}</div>
    </Button>
  );
}

const sideMenuStyles = tv({
  base: "flex flex-col pr-2 py-4 transition-all duration-300 items-start shrink-0 grow-0",
  variants: {
    isCollapsed: {
      true: "w-[72px] gap-2 pl-2 ease-out",
      false: "w-72 gap-4 pl-8 ease-in"
    }
  }
});

const chevronStyles = tv({
  base: "w-4 h-4 transition-all duration-300",
  variants: {
    isCollapsed: {
      true: "transform rotate-180 ease-out",
      false: "transform rotate-0 ease-in"
    }
  }
});

const logoStyles = tv({
  base: "self-start opacity-100 transition-all duration-300",
  variants: {
    isCollapsed: {
      true: "h-0 opacity-0 ease-out",
      false: "h-8 ease-in"
    }
  }
});

export function SideMenu() {
  const [isCollapsed, setIsCollapsed] = useState(() => !window.matchMedia("(min-width: 1024px)").matches);

  const toggleCollapse = () => {
    setIsCollapsed((v) => !v);
  };

  return (
    <collapsedContext.Provider value={isCollapsed}>
      <div className={sideMenuStyles({ isCollapsed })}>
        <div className="flex items-center self-end">
          <Button variant="ghost" size="icon" onPress={toggleCollapse}>
            <ChevronsLeftIcon className={chevronStyles({ isCollapsed })} />
          </Button>
          <div className="border border-border h-8" />
        </div>
        <img src={logoWrap} alt="Logo Wrap" className={logoStyles({ isCollapsed })} />
        <MenuButton icon={HomeIcon} label="Home" />
        <MenuSeparator>Organisation</MenuSeparator>
        <MenuButton icon={CircleUserIcon} label="Account" />
        <MenuButton icon={UsersRoundIcon} label="Users" />
      </div>
    </collapsedContext.Provider>
  );
}

const menuSeparatorStyles = tv({
  base: "text-muted-foreground border-b-0 font-semibold uppercase transition-all duration-300 leading-4",
  variants: {
    isCollapsed: {
      true: "h-0 w-6 text-muted-foreground/0 border-b-4 border-border/100 text-[0px] pt-0 self-center ease-out",
      false: "h-8 w-full border-border/0 text-xs pt-4 ease-in"
    }
  }
});

type MenuSeparatorProps = {
  children: React.ReactNode;
};

function MenuSeparator({ children }: Readonly<MenuSeparatorProps>) {
  const isCollapsed = useContext(collapsedContext);
  return (
    <div className="pl-4">
      <div className={menuSeparatorStyles({ isCollapsed })}>{children}</div>
    </div>
  );
}
