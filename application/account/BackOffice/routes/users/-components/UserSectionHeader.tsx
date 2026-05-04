import type { LucideIcon } from "lucide-react";
import type { ReactNode } from "react";

interface UserSectionHeaderProps {
  icon: LucideIcon;
  title: ReactNode;
  description?: ReactNode;
}

export function UserSectionHeader({ icon: Icon, title, description }: Readonly<UserSectionHeaderProps>) {
  return (
    <div className="flex flex-wrap items-center gap-2">
      <h4 className="flex items-center gap-2">
        <Icon className="size-4 text-muted-foreground" aria-hidden="true" />
        {title}
      </h4>
      {description && (
        <span className="text-sm text-muted-foreground">
          <span aria-hidden="true">{"· "}</span>
          {description}
        </span>
      )}
    </div>
  );
}
