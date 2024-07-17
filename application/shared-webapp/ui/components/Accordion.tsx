/**
 * Status: Beta
 * ref: https://react-spectrum.adobe.com/react-spectrum
 * ref: https://ui.shadcn.com/docs/components/accordion
 */
import type { AccordionItemAriaProps, AriaAccordionProps } from "@react-aria/accordion";
import { useAccordion, useAccordionItem } from "@react-aria/accordion";
import { type TreeState, useTreeState } from "@react-stately/tree";
import { ChevronDown } from "lucide-react";
import {
  type AriaRole,
  type ButtonHTMLAttributes,
  type CSSProperties,
  createContext,
  forwardRef,
  useContext,
  useMemo,
  useRef
} from "react";
import { useFocusRing } from "react-aria";
import { twMerge } from "tailwind-merge";
import { tv } from "tailwind-variants";
import { focusRing } from "./focusRing";

export { Item } from "@react-stately/collections";

type AccordionProps<T> = {
  className?: string;
} & AriaAccordionProps<T>;

export function Accordion<T extends object>({ className, ...props }: AccordionProps<T>) {
  const state = useTreeState<T>(props);
  const ref = useRef<HTMLDivElement>(null) as React.RefObject<HTMLDivElement>; // Note(raix): Remove when fixed in react-aria
  const { accordionProps } = useAccordion<T>(props, state, ref);

  return (
    <div ref={ref} {...accordionProps} className={className}>
      {[...state.collection].map((item) => (
        <AccordionItemInstance<T> key={item.key} item={item} state={state} />
      ))}
    </div>
  );
}

type AccordionItemContext = {
  isOpen?: boolean;
  isDisabled?: boolean;
};

const accordionItemContext = createContext<AccordionItemContext>({});
const useAccordionItemContext = () => useContext(accordionItemContext);

type AccordionItemInstanceProps<T> = {
  state: TreeState<T>;
} & AccordionItemAriaProps<T>;

function AccordionItemInstance<T>({ state, ...props }: AccordionItemInstanceProps<T>) {
  const ref = useRef<HTMLButtonElement>(null) as React.RefObject<HTMLButtonElement>; // Note(raix): Remove when fixed in react-aria
  const { item } = props;
  const { buttonProps, regionProps } = useAccordionItem<T>(props, state, ref);
  const isOpen = state.expandedKeys.has(item.key);
  const isDisabled = state.disabledKeys.has(item.key);

  const accordionItemRenderProps = useMemo<AccordionItemContext>(() => ({ isOpen, isDisabled }), [isOpen, isDisabled]);

  return (
    <accordionItemContext.Provider value={accordionItemRenderProps}>
      <AccordionItemContainer>
        <AccordionTrigger {...buttonProps} ref={ref}>
          {item.props.title}
        </AccordionTrigger>
        <AccordionContent {...regionProps}>{item.props.children}</AccordionContent>
      </AccordionItemContainer>
    </accordionItemContext.Provider>
  );
}

const buttonStyles = tv({
  extend: focusRing,
  base: "flex flex-1 items-center justify-between py-4 font-medium hover:underline rounded-md",
  variants: {
    isDisabled: {
      true: "opacity-50 pointer-events-none hover:underline-none"
    }
  }
});

type AccordionTriggerProps = {
  className?: string;
  children: React.ReactNode;
} & ButtonHTMLAttributes<HTMLButtonElement>;

const AccordionTrigger = forwardRef<HTMLButtonElement, AccordionTriggerProps>(
  ({ className, children, ...props }, ref) => {
    const { focusProps, isFocusVisible } = useFocusRing();
    const { isOpen, isDisabled } = useAccordionItemContext();
    return (
      <div className="flex">
        <button
          {...props}
          {...focusProps}
          ref={ref}
          className={buttonStyles({ isFocusVisible, isDisabled, className })}
        >
          {children}
          <ChevronDown
            aria-hidden="true"
            className={twMerge("h-4 w-4 shrink-0 transition-transform duration-200", isOpen && "rotate-180")}
          />
        </button>
      </div>
    );
  }
);

const contentStyles = tv({
  base: "overflow-hidden text-sm transition-all",
  variants: {
    isOpen: {
      true: "animate-accordion-down h-auto",
      false: "animate-accordion-up h-0"
    },
    isDisabled: {
      true: "text-muted-foreground"
    }
  },
  defaultVariants: {
    isOpen: false
  }
});

type AccordionContentProps = {
  id?: string;
  role?: AriaRole;
  tabIndex?: number;
  style?: CSSProperties;
  className?: string;
  children: React.ReactNode;
};

function AccordionContent({ className, children, ...props }: Readonly<AccordionContentProps>) {
  const { isOpen, isDisabled } = useAccordionItemContext();

  return (
    <div {...props} aria-hidden={!isOpen} className={contentStyles({ isOpen, isDisabled })}>
      <div className={twMerge("pt-1 pb-4", className)}>{children}</div>
    </div>
  );
}

type AccordionItemContainerProps = {
  className?: string;
  children: React.ReactNode;
};

function AccordionItemContainer({ className, children }: Readonly<AccordionItemContainerProps>) {
  return <div className={twMerge("border-b", className)}>{children}</div>;
}
