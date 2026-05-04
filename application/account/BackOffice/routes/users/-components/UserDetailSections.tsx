import type { components } from "@/shared/lib/api/client";

import { UserKpiCards } from "./UserKpiCards";
import { UserLoginHistorySection } from "./UserLoginHistorySection";
import { UserSessionsSection } from "./UserSessionsSection";
import { UserTelemetrySection } from "./UserTelemetrySection";
import { UserTenantsSection } from "./UserTenantsSection";

type BackOfficeUserDetailResponse = components["schemas"]["BackOfficeUserDetailResponse"];

interface UserDetailSectionsProps {
  userId: string;
  user: BackOfficeUserDetailResponse | undefined;
  isLoading: boolean;
}

export function UserDetailSections({ userId, user, isLoading }: Readonly<UserDetailSectionsProps>) {
  return (
    <div className="mt-6 flex flex-col gap-6">
      <UserKpiCards user={user} userId={userId} isLoading={isLoading} />
      <UserTenantsSection user={user} />
      <UserSessionsSection userId={userId} />
      <UserLoginHistorySection userId={userId} />
      <UserTelemetrySection />
    </div>
  );
}
