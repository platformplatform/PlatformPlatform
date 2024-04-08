import { Input as RACInput } from "react-aria-components";
import type { InputProps } from "react-aria-components";
import { AlertCircleIcon } from "lucide-react";
import { composeTailwindRenderProps } from "./components/utils";

interface DomainInputProps extends InputProps {
  domain: string;
  showDomainTakenIcon?: boolean;
}

export function DomainInput({ domain, showDomainTakenIcon, ...props }: Readonly<DomainInputProps>) {
  return (
    <div className="relative flex w-full outline outline-0 border border-neutral-200 rounded">
      <RACInput {...props} className={composeTailwindRenderProps(props.className, "px-2 py-1.5 flex-1 w-full rounded bg-white dark:bg-zinc-900 text-sm text-gray-800 dark:text-zinc-200 disabled:text-gray-200 dark:disabled:text-zinc-600")} />
      <div className="pr-2 flex items-center text-sm text-gray-400 dark:text-zinc-600">{domain}</div>
      {showDomainTakenIcon && <AlertCircleIcon className="h-4 w-4 text-red-500" />}
    </div>
  );
}
