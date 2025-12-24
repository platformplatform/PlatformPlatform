/**
 * ref: https://react-spectrum.adobe.com/react-aria-tailwind-starter/?path=/docs/searchfield--docs
 */
import { SearchIcon, XIcon } from "lucide-react";
import {
  Input as AriaInput,
  SearchField as AriaSearchField,
  type SearchFieldProps as AriaSearchFieldProps,
  type ValidationResult
} from "react-aria-components";
import { Button } from "./Button";
import { Description } from "./Description";
import { FieldError } from "./FieldError";
import { FieldGroup } from "./fieldStyles";
import { LabelWithTooltip } from "./LabelWithTooltip";
import { composeTailwindRenderProps } from "./utils";

export interface SearchFieldProps extends AriaSearchFieldProps {
  label?: string;
  description?: string;
  placeholder?: string;
  errorMessage?: string | ((validation: ValidationResult) => string);
  tooltip?: string;
}

export function SearchField({
  label,
  description,
  errorMessage,
  tooltip,
  placeholder,
  ...props
}: Readonly<SearchFieldProps>) {
  return (
    <AriaSearchField
      {...props}
      className={composeTailwindRenderProps(props.className, "group flex min-w-[40px] flex-col gap-3")}
    >
      {label && <LabelWithTooltip tooltip={tooltip}>{label}</LabelWithTooltip>}
      <FieldGroup>
        <SearchIcon
          aria-hidden={true}
          className="ml-2 h-4 min-h-[16px] w-4 min-w-[16px] text-muted-foreground group-disabled:opacity-50 forced-colors:text-[ButtonText] forced-colors:group-disabled:text-[GrayText]"
        />
        <AriaInput
          placeholder={placeholder}
          className="h-9 w-full min-w-0 border-0 bg-transparent px-2.5 py-1 text-base shadow-none outline-none placeholder:text-muted-foreground focus:ring-0 md:text-sm [&::-webkit-search-cancel-button]:hidden"
        />
        <Button variant="ghost" size="icon" className="mr-1 w-6 group-empty:invisible">
          <XIcon aria-hidden={true} className="h-4 w-4" />
        </Button>
      </FieldGroup>
      {description && <Description>{description}</Description>}
      <FieldError>{errorMessage}</FieldError>
    </AriaSearchField>
  );
}
