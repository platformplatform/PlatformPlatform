import type { ComponentProps } from "react";

import { t } from "@lingui/core/macro";
import { Trans } from "@lingui/react/macro";
import { trackInteraction } from "@repo/infrastructure/applicationInsights/ApplicationInsightsProvider";
import { Button } from "@repo/ui/components/Button";
import { DateRangePicker } from "@repo/ui/components/DateRangePicker";
import {
  Dialog,
  DialogBody,
  DialogClose,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle
} from "@repo/ui/components/Dialog";
import { Field, FieldLabel } from "@repo/ui/components/Field";
import { InputGroup, InputGroupAddon, InputGroupButton, InputGroupInput } from "@repo/ui/components/InputGroup";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@repo/ui/components/Select";
import { format } from "date-fns";
import { SearchIcon, XIcon } from "lucide-react";

import { UserRole, UserStatus } from "@/shared/lib/api/client";
import { getUserRoleLabel } from "@/shared/lib/api/userRole";
import { getUserStatusLabel } from "@/shared/lib/api/userStatus";

import type { FilterUpdateFn } from "./userQueryingTypes";

interface UserFilterDialogProps {
  isOpen: boolean;
  onOpenChange: (open: boolean) => void;
  search: string;
  onSearchChange: (value: string) => void;
  dateRange: ComponentProps<typeof DateRangePicker>["value"];
  userRole: UserRole | undefined;
  userStatus: UserStatus | undefined;
  hasSearch: boolean;
  activeFilterCount: number;
  updateFilter: FilterUpdateFn;
  onClearAll: () => void;
}

export function UserFilterDialog({
  isOpen,
  onOpenChange,
  search,
  onSearchChange,
  dateRange,
  userRole,
  userStatus,
  hasSearch,
  activeFilterCount,
  updateFilter,
  onClearAll
}: Readonly<UserFilterDialogProps>) {
  return (
    <Dialog open={isOpen} onOpenChange={onOpenChange} trackingTitle="User filters">
      <DialogContent className="sm:w-dialog-sm">
        <DialogHeader>
          <DialogTitle>
            <Trans>Filters</Trans>
          </DialogTitle>
        </DialogHeader>

        <DialogBody>
          <Field>
            <FieldLabel>{t`Search`}</FieldLabel>
            <InputGroup>
              <InputGroupAddon>
                <SearchIcon />
              </InputGroupAddon>
              <InputGroupInput
                type="text"
                role="searchbox"
                placeholder={t`Search`}
                value={search}
                onChange={(e) => onSearchChange(e.target.value)}
                onKeyDown={(e) => e.key === "Escape" && search && onSearchChange("")}
              />
              {search && (
                <InputGroupAddon align="inline-end">
                  <InputGroupButton onClick={() => onSearchChange("")} size="icon-xs" aria-label={t`Clear search`}>
                    <XIcon />
                  </InputGroupButton>
                </InputGroupAddon>
              )}
            </InputGroup>
          </Field>

          <DateRangePicker
            value={dateRange}
            onChange={(range) => {
              trackInteraction("User filters", "interaction", "Date filter");
              updateFilter({
                startDate: range ? format(range.start, "yyyy-MM-dd") : undefined,
                endDate: range ? format(range.end, "yyyy-MM-dd") : undefined
              });
            }}
            label={t`Modified date`}
            placeholder={t`Select dates`}
            className="w-full"
          />

          <Field className="flex w-full flex-col">
            <FieldLabel>{t`User role`}</FieldLabel>
            <Select
              value={userRole ?? ""}
              onValueChange={(value) => {
                trackInteraction("User filters", "interaction", "Role filter");
                updateFilter({ userRole: (value as UserRole) || undefined });
              }}
            >
              <SelectTrigger className="w-full" aria-label={t`User role`}>
                <SelectValue>
                  {(value: string) => (value ? getUserRoleLabel(value as UserRole) : t`Any role`)}
                </SelectValue>
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="">
                  <Trans>Any role</Trans>
                </SelectItem>
                {Object.values(UserRole).map((role) => (
                  <SelectItem value={role} key={role}>
                    {getUserRoleLabel(role)}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </Field>

          <Field className="flex w-full flex-col">
            <FieldLabel>{t`User status`}</FieldLabel>
            <Select
              value={userStatus ?? ""}
              onValueChange={(value) => {
                trackInteraction("User filters", "interaction", "Status filter");
                updateFilter({ userStatus: (value as UserStatus) || undefined });
              }}
            >
              <SelectTrigger className="w-full" aria-label={t`User status`}>
                <SelectValue>
                  {(value: string) => (value ? getUserStatusLabel(value as UserStatus) : t`Any status`)}
                </SelectValue>
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="">
                  <Trans>Any status</Trans>
                </SelectItem>
                {Object.values(UserStatus).map((status) => (
                  <SelectItem value={status} key={status}>
                    {getUserStatusLabel(status)}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </Field>
        </DialogBody>

        <DialogFooter>
          <Button variant="secondary" onClick={onClearAll} disabled={activeFilterCount === 0 && !hasSearch}>
            <Trans>Clear</Trans>
          </Button>
          <DialogClose render={<Button variant="default" />}>
            <Trans>OK</Trans>
          </DialogClose>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
