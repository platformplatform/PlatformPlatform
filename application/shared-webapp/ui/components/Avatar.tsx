import { useState, type SyntheticEvent } from "react";

type AvatarProps = {
  firstName: string | undefined;
  lastName: string | undefined;
  avatarUrl?: string | null;
};

export function Avatar({ firstName, lastName, avatarUrl }: AvatarProps) {
  const [imageFailed, setImageFailed] = useState(false);
  const handleError = () => setImageFailed(true);
  return avatarUrl && imageFailed === false ? (
    <img
      src={avatarUrl}
      alt="User avatar"
      className="mr-2 w-10 h-10 rounded-full bg-transparent"
      onError={handleError}
    />
  ) : (
    <div className="w-10 h-10 min-w-[2.5rem] min-h-[2.5rem] rounded-full bg-gray-200 mr-2 flex items-center justify-center text-sm font-semibold uppercase">
      {getInitials(firstName, lastName)}
    </div>
  );
}

function getInitials(firstName: string | undefined, lastName: string | undefined) {
  if (!firstName && !lastName) return "N/A";
  const firstInitial = firstName ? firstName[0].toUpperCase() : "";
  const lastInitial = lastName ? lastName[0].toUpperCase() : "";
  return `${firstInitial}${lastInitial}`;
}
