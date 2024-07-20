import { createContext, useContext, useState } from "react";
import { ChevronsLeftIcon, CircleUserIcon, HomeIcon, type LucideIcon, UsersRoundIcon } from "lucide-react";
import { Button } from "./Button";
import { tv } from "tailwind-variants";
import logoMarkUrl from "../images/logo-mark.svg";
import logoWrapUrl from "../images/logo-wrap.svg";
import { Tooltip, TooltipTrigger } from "./Tooltip";

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
    <TooltipTrigger>
      <Button variant="ghost" className={menuButtonStyles({ isCollapsed })}>
        <Icon className="w-6 h-6 shrink-0 grow-0" />
        <div className={menuTextStyles({ isCollapsed })}>{label}</div>
      </Button>
      {isCollapsed && <Tooltip placement="right">{label}</Tooltip>}
    </TooltipTrigger>
  );
}

const sideMenuStyles = tv({
  base: "relative flex flex-col pr-2 py-4 transition-all duration-300 items-start shrink-0 grow-0",
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

const logoWrapStyles = tv({
  base: "self-start  transition-all duration-300",
  variants: {
    isCollapsed: {
      true: "h-8 opacity-0 ease-out",
      false: "h-8 ease-in opacity-100"
    }
  }
});

const logoMarkStyles = tv({
  base: "self-start transition-all duration-300",
  variants: {
    isCollapsed: {
      true: "h-8 opacity-100 ease-in",
      false: "h-8 opacity-0 ease-out"
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
        <div className="h-20">
          <Button
            variant="ghost"
            size="sm"
            onPress={toggleCollapse}
            className="absolute top-3.5 right-0 hover:bg-transparent hover:text-muted-foreground border-r-2 border-border rounded-r-none"
          >
            <ChevronsLeftIcon className={chevronStyles({ isCollapsed })} />
          </Button>
          <div className="pr-8">
            <img src={logoWrapUrl} alt="Logo Wrap" className={logoWrapStyles({ isCollapsed })} />
          </div>
          <div className="flex pl-3 pt-4">
            <img src={logoMarkUrl} alt="Logo" className={logoMarkStyles({ isCollapsed })} />
          </div>
        </div>
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
