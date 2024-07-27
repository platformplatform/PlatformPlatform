/**
 * ref: https://react-spectrum.adobe.com/react-aria-tailwind-starter/?path=/docs/searchfield--docs
 */
import { SearchIcon, XIcon } from "lucide-react";
import {
  SearchField as AriaSearchField,
  type SearchFieldProps as AriaSearchFieldProps,
  type ValidationResult
} from "react-aria-components";
import { Button } from "./Button";
import { Description } from "./Description";
import { FieldGroup } from "./Field";
import { FieldError } from "./FieldError";
import { Input } from "./Input";
import { Label } from "./Label";
import { composeTailwindRenderProps } from "./utils";

export interface SearchFieldProps extends AriaSearchFieldProps {
  label?: string;
  description?: string;
  placeholder?: string;
  errorMessage?: string | ((validation: ValidationResult) => string);
}

export function SearchField({ label, description, errorMessage, placeholder, ...props }: Readonly<SearchFieldProps>) {
  return (
    <AriaSearchField
      {...props}
      className={composeTailwindRenderProps(props.className, "group flex min-w-[40px] flex-col gap-1")}
    >
      {label && <Label>{label}</Label>}
      <FieldGroup>
        <SearchIcon
          aria-hidden
          className="ml-2 h-4 w-4 text-muted-foreground group-disabled:opacity-50 forced-colors:text-[ButtonText] forced-colors:group-disabled:text-[GrayText]"
        />
        <Input placeholder={placeholder} isEmbedded className="[&::-webkit-search-cancel-button]:hidden" />
        <Button variant="icon" className="mr-1 w-6 group-empty:invisible">
          <XIcon aria-hidden className="h-4 w-4" />
        </Button>
      </FieldGroup>
      {description && <Description>{description}</Description>}
      <FieldError>{errorMessage}</FieldError>
    </AriaSearchField>
  );
}
