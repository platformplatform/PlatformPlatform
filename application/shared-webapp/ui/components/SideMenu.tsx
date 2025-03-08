import type { Href } from "@react-types/shared";
import { useRouter } from "@tanstack/react-router";
import { ChevronsLeftIcon, type LucideIcon } from "lucide-react";
import type React from "react";
import { createContext, useContext } from "react";
import { tv } from "tailwind-variants";
import { useLocalStorage } from "../hooks/useLocalStorage";
import logoMarkUrl from "../images/logo-mark.svg";
import logoWrapUrl from "../images/logo-wrap.svg";
import { Button } from "./Button";
import { Dialog, DialogTrigger } from "./Dialog";
import { Modal } from "./Modal";
import { Tooltip, TooltipTrigger } from "./Tooltip";

const collapsedContext = createContext(false);

const menuButtonStyles = tv({
  base: "flex w-full justify-start font-normal text-base transition-all duration-300",
  variants: {
    isCollapsed: {
      true: "gap-0 ease-out",
      false: "gap-4 ease-in"
    }
  }
});

const menuTextStyles = tv({
  base: "text-start text-foreground transition-all duration-300",
  variants: {
    isCollapsed: {
      true: "w-0 text-xs opacity-0 ease-out",
      false: "w-fit text-base opacity-100 ease-in"
    }
  }
});

type MenuButtonProps = {
  icon: LucideIcon;
  label: string;
  isDisabled?: boolean;
} & (
  | {
      forceReload?: false;
      href: Href;
    }
  | {
      forceReload: true;
      href: string;
    }
);

export function MenuButton({
  icon: Icon,
  label,
  href: to,
  isDisabled = false,
  forceReload = false
}: Readonly<MenuButtonProps>) {
  const isCollapsed = useContext(collapsedContext);
  const { navigate } = useRouter();
  const onPress = () => {
    if (to == null) {
      return;
    }
    if (forceReload) {
      window.location.href = to;
    } else {
      navigate({ to });
    }
  };

  return (
    <TooltipTrigger delay={300}>
      <Button variant="link" className={menuButtonStyles({ isCollapsed })} onPress={onPress} isDisabled={isDisabled}>
        <Icon className="h-6 w-6 shrink-0 grow-0" />
        <div className={menuTextStyles({ isCollapsed })}>{label}</div>
      </Button>
      {isCollapsed && <Tooltip placement="right">{label}</Tooltip>}
    </TooltipTrigger>
  );
}

const sideMenuStyles = tv({
  base: "relative hidden shrink-0 grow-0 flex-col items-start py-4 pr-2 transition-all duration-300 sm:flex",
  variants: {
    isCollapsed: {
      true: "w-[72px] gap-2 pl-2 ease-out",
      false: "w-72 gap-4 pl-8 ease-in"
    }
  }
});

const chevronStyles = tv({
  base: "h-4 w-4 transition-all duration-300",
  variants: {
    isCollapsed: {
      true: "rotate-180 transform ease-out",
      false: "rotate-0 transform ease-in"
    }
  }
});

const logoWrapStyles = tv({
  base: "self-start transition-all duration-300",
  variants: {
    isCollapsed: {
      true: "h-8 opacity-0 ease-out",
      false: "h-8 opacity-100 ease-in"
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

type SideMenuProps = {
  children: React.ReactNode;
  ariaLabel: string;
};

export function SideMenu({ children, ariaLabel }: Readonly<SideMenuProps>) {
  const [isCollapsed, setIsCollapsed] = useLocalStorage<boolean>(
    !window.matchMedia("(min-width: 1024px)").matches,
    "side-menu-collapsed"
  );

  const toggleCollapse = () => {
    setIsCollapsed((v: boolean) => !v);
  };

  return (
    <>
      <collapsedContext.Provider value={isCollapsed}>
        <div className={sideMenuStyles({ isCollapsed })}>
          <div className="h-20">
            <Button
              variant="ghost"
              size="sm"
              onPress={toggleCollapse}
              className="absolute top-3.5 right-0 rounded-r-none border-border border-r-2 hover:bg-transparent hover:text-muted-foreground"
              aria-label={ariaLabel}
            >
              <ChevronsLeftIcon className={chevronStyles({ isCollapsed })} />
            </Button>
            <div className="pr-8">
              <img src={logoWrapUrl} alt="Logo Wrap" className={logoWrapStyles({ isCollapsed })} />
            </div>
            <div className="flex pt-4 pl-3">
              <img src={logoMarkUrl} alt="Logo" className={logoMarkStyles({ isCollapsed })} />
            </div>
          </div>
          {children}
        </div>
      </collapsedContext.Provider>
      <collapsedContext.Provider value={false}>
        <div className="absolute right-2 bottom-2 z-50 sm:hidden">
          <DialogTrigger>
            <Button aria-label="Help" variant="icon">
              <img src={logoMarkUrl} alt="Logo" className="h-8 w-8" />
            </Button>
            <Modal position="left" fullSize={true}>
              <Dialog className="w-60">
                <div className="pb-8">
                  <img src={logoWrapUrl} alt="Logo Wrap" />
                </div>
                {children}
              </Dialog>
            </Modal>
          </DialogTrigger>
        </div>
      </collapsedContext.Provider>
    </>
  );
}

const sideMenuSeparatorStyles = tv({
  base: "border-b-0 font-semibold text-muted-foreground uppercase leading-4 transition-all duration-300",
  variants: {
    isCollapsed: {
      true: "h-0 w-6 self-center border-border/100 border-b-4 pt-0 text-[0px] text-muted-foreground/0 ease-out",
      false: "h-8 w-full border-border/0 pt-4 text-xs ease-in"
    }
  }
});

type SideMenuSeparatorProps = {
  children: React.ReactNode;
};

export function SideMenuSeparator({ children }: Readonly<SideMenuSeparatorProps>) {
  const isCollapsed = useContext(collapsedContext);
  return (
    <div className="pl-4">
      <div className={sideMenuSeparatorStyles({ isCollapsed })}>{children}</div>
    </div>
  );
}

export function SideMenuSpacer() {
  return <div className="grow" />;
}
