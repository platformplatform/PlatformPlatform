export interface TeamDetails {
  id: string;
  name: string;
  description: string;
  memberCount: number;
}

export const mockTeams: TeamDetails[] = [
  {
    id: "team-1",
    name: "Engineering",
    description: "Product development team",
    memberCount: 12
  },
  {
    id: "team-2",
    name: "Marketing",
    description: "Marketing and communications",
    memberCount: 5
  },
  {
    id: "team-3",
    name: "Sales",
    description: "Sales and business development",
    memberCount: 8
  },
  {
    id: "team-4",
    name: "Support",
    description: "Customer success and support",
    memberCount: 15
  },
  {
    id: "team-5",
    name: "Design",
    description: "UX and UI design team",
    memberCount: 7
  }
];
