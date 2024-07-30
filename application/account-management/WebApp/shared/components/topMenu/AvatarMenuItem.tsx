import { Avatar } from "@repo/ui/components/Avatar";

type AvatarMenuItemProps = {
  name: string;
  title?: string;
  avatarUrl?: string;
  initials?: string;
};

export function AvatarMenuItem({ name, title, avatarUrl, initials }: Readonly<AvatarMenuItemProps>) {
  return (
    <div className="flex flex-row items-center gap-2">
      <Avatar avatarUrl={avatarUrl} initials={initials ?? ""} isRound size="sm" />
      <div className="flex flex-col">
        <h2>{name}</h2>
        <p className="text-muted-foreground text-sm font-normal">{title}</p>
      </div>
    </div>
  );
}
