import type { Schemas, TeamMemberRole } from "@/shared/lib/api/client";

type TeamMember = Schemas["TeamMemberDetails"];

export interface PickerUser {
  userId: string;
  email: string;
  firstName: string | null;
  lastName: string | null;
  title: string | null;
  avatarUrl: string | null;
  role: TeamMemberRole | null;
}

export function matchesSearch(user: PickerUser, query: string) {
  if (!query) {
    return true;
  }
  const haystack = `${user.firstName ?? ""} ${user.lastName ?? ""} ${user.email} ${user.title ?? ""}`.toLowerCase();
  return haystack.includes(query);
}

export function compareUsers(a: PickerUser, b: PickerUser) {
  const aName = `${a.firstName ?? ""} ${a.lastName ?? ""}`.trim() || a.email;
  const bName = `${b.firstName ?? ""} ${b.lastName ?? ""}`.trim() || b.email;
  return aName.localeCompare(bName);
}

export function memberToPickerUser(member: TeamMember): PickerUser {
  return {
    userId: member.userId,
    email: member.email,
    firstName: member.firstName,
    lastName: member.lastName,
    title: member.title,
    avatarUrl: member.avatarUrl,
    role: member.role
  };
}

interface AllUser {
  id: string;
  email: string;
  firstName: string | null;
  lastName: string | null;
  title: string | null;
  avatarUrl: string | null;
}

export function buildPickerUsers(allUsers: AllUser[], originalMembers: TeamMember[] | undefined): PickerUser[] {
  const memberLookup = new Map<string, TeamMember>();
  for (const member of originalMembers ?? []) {
    memberLookup.set(member.userId, member);
  }
  const map = new Map<string, PickerUser>();
  for (const user of allUsers) {
    const member = memberLookup.get(user.id);
    map.set(user.id, {
      userId: user.id,
      email: user.email,
      firstName: user.firstName,
      lastName: user.lastName,
      title: user.title,
      avatarUrl: user.avatarUrl,
      role: member?.role ?? null
    });
  }
  for (const member of originalMembers ?? []) {
    if (!map.has(member.userId)) {
      map.set(member.userId, memberToPickerUser(member));
    }
  }
  return Array.from(map.values());
}

export function computePendingDiff(
  originalMembers: TeamMember[],
  pendingMemberIds: Set<string>
): { addUserIds: string[]; removeUserIds: string[] } {
  const originalIds = new Set(originalMembers.map((member) => member.userId));
  const addUserIds = Array.from(pendingMemberIds).filter((id) => !originalIds.has(id));
  const removeUserIds = Array.from(originalIds).filter((id) => !pendingMemberIds.has(id));
  return { addUserIds, removeUserIds };
}

export function hasPendingChanges(originalMembers: TeamMember[] | undefined, pendingMemberIds: Set<string>): boolean {
  if (!originalMembers) {
    return false;
  }
  if (originalMembers.length !== pendingMemberIds.size) {
    return true;
  }
  for (const member of originalMembers) {
    if (!pendingMemberIds.has(member.userId)) {
      return true;
    }
  }
  return false;
}
