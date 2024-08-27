import { Avatar } from "@repo/ui/components/Avatar";

type AvatarMenuItemProps = {
  name: string;
  title?: string;
  email?: string;
  avatarUrl?: string;
  initials?: string;
};

export function AvatarMenuItem({ name, title, email, avatarUrl, initials }: Readonly<AvatarMenuItemProps>) {
  return (
    <div className="flex flex-row items-center gap-2">
      <Avatar avatarUrl={avatarUrl} initials={initials ?? ""} isRound size="sm" />
      <div className="flex flex-col my-1">
        <h2>{name}</h2>
        <p className="text-muted-foreground">{title ?? email}</p>
      </div>
    </div>
  );
}
