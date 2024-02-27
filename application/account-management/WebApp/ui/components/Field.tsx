import type { FieldErrorProps, GroupProps, InputProps, LabelProps, TextProps } from "react-aria-components";
import { Group, FieldError as RACFieldError, Input as RACInput, Label as RACLabel, Text, composeRenderProps } from "react-aria-components";
import { twMerge } from "tailwind-merge";
import { tv } from "tailwind-variants";
import { composeTailwindRenderProps, focusRing } from "./utils";

export function Label(props: Readonly<LabelProps>) {
  return <RACLabel {...props} className={twMerge("text-sm text-gray-500 dark:text-zinc-400 font-medium cursor-default w-fit", props.className)} />;
}

export function Description(props: Readonly<TextProps>) {
  return <Text {...props} slot="description" className={twMerge("text-sm text-gray-600", props.className)} />;
}

export function FieldError(props: Readonly<FieldErrorProps>) {
  return <RACFieldError {...props} className={composeTailwindRenderProps(props.className, "text-sm text-red-600 forced-colors:text-[Mark]")} />;
}

// eslint-disable-next-line react-refresh/only-export-components
export const fieldBorderStyles = tv({
  variants: {
    isFocusWithin: {
      false: "border-gray-300 dark:border-zinc-500 forced-colors:border-[ButtonBorder]",
      true: "border-gray-600 dark:border-zinc-300 forced-colors:border-[Highlight]",
    },
    isInvalid: {
      true: "border-red-600 dark:border-red-600 forced-colors:border-[Mark]",
    },
    isDisabled: {
      true: "border-gray-200 dark:border-zinc-700 forced-colors:border-[GrayText]",
    },
  },
});

// eslint-disable-next-line react-refresh/only-export-components
export const fieldGroupStyles = tv({
  extend: focusRing,
  base: "group flex items-center h-9 bg-white dark:bg-zinc-900 forced-colors:bg-[Field] border-2 rounded-lg overflow-hidden",
  variants: fieldBorderStyles.variants,
});

export function FieldGroup(props: Readonly<GroupProps>) {
  return (
    <Group
      {...props}
      className={composeRenderProps(props.className, (className, renderProps) => {
        return fieldGroupStyles({ ...renderProps, className });
      })}
    />
  );
}

export function Input(props: Readonly<InputProps>) {
  return <RACInput {...props} className={composeTailwindRenderProps(props.className, "px-2 py-1.5 flex-1 min-w-0 outline outline-0 border border-neutral-200 rounded bg-white dark:bg-zinc-900 text-sm text-gray-800 dark:text-zinc-200 disabled:text-gray-200 dark:disabled:text-zinc-600")} />;
}
