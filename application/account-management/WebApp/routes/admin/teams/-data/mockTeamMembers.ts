export interface TeamMemberDetails {
  id: string;
  userId: string;
  name: string;
  email: string;
  title: string;
  avatarUrl: string | null;
  role: "Admin" | "Member";
}

export const mockTeamMembers: Record<string, TeamMemberDetails[]> = {
  "team-1": [
    {
      id: "member-1-1",
      userId: "user-1",
      name: "John Doe",
      email: "john@example.com",
      title: "Senior Developer",
      avatarUrl: null,
      role: "Admin"
    },
    {
      id: "member-1-2",
      userId: "user-2",
      name: "Jane Smith",
      email: "jane@example.com",
      title: "Product Manager",
      avatarUrl: null,
      role: "Member"
    },
    {
      id: "member-1-3",
      userId: "user-3",
      name: "Bob Wilson",
      email: "bob@example.com",
      title: "Backend Engineer",
      avatarUrl: null,
      role: "Member"
    },
    {
      id: "member-1-4",
      userId: "user-4",
      name: "Alice Johnson",
      email: "alice@example.com",
      title: "Frontend Engineer",
      avatarUrl: null,
      role: "Member"
    },
    {
      id: "member-1-5",
      userId: "user-5",
      name: "Charlie Brown",
      email: "charlie@example.com",
      title: "DevOps Engineer",
      avatarUrl: null,
      role: "Admin"
    },
    {
      id: "member-1-6",
      userId: "user-6",
      name: "Diana Prince",
      email: "diana@example.com",
      title: "QA Engineer",
      avatarUrl: null,
      role: "Member"
    },
    {
      id: "member-1-7",
      userId: "user-7",
      name: "Eve Anderson",
      email: "eve@example.com",
      title: "UX Designer",
      avatarUrl: null,
      role: "Member"
    },
    {
      id: "member-1-8",
      userId: "user-8",
      name: "Frank Miller",
      email: "frank@example.com",
      title: "Tech Lead",
      avatarUrl: null,
      role: "Admin"
    },
    {
      id: "member-1-9",
      userId: "user-9",
      name: "Grace Lee",
      email: "grace@example.com",
      title: "Software Engineer",
      avatarUrl: null,
      role: "Member"
    },
    {
      id: "member-1-10",
      userId: "user-10",
      name: "Henry Davis",
      email: "henry@example.com",
      title: "Data Engineer",
      avatarUrl: null,
      role: "Member"
    },
    {
      id: "member-1-11",
      userId: "user-11",
      name: "Ivy Chen",
      email: "ivy@example.com",
      title: "Mobile Developer",
      avatarUrl: null,
      role: "Member"
    },
    {
      id: "member-1-12",
      userId: "user-12",
      name: "Jack Taylor",
      email: "jack@example.com",
      title: "Security Engineer",
      avatarUrl: null,
      role: "Member"
    }
  ],
  "team-2": [
    {
      id: "member-2-1",
      userId: "user-13",
      name: "Kate Martinez",
      email: "kate@example.com",
      title: "Marketing Director",
      avatarUrl: null,
      role: "Admin"
    },
    {
      id: "member-2-2",
      userId: "user-14",
      name: "Leo Garcia",
      email: "leo@example.com",
      title: "Content Strategist",
      avatarUrl: null,
      role: "Member"
    },
    {
      id: "member-2-3",
      userId: "user-15",
      name: "Mia Rodriguez",
      email: "mia@example.com",
      title: "Social Media Manager",
      avatarUrl: null,
      role: "Member"
    },
    {
      id: "member-2-4",
      userId: "user-16",
      name: "Noah Thompson",
      email: "noah@example.com",
      title: "SEO Specialist",
      avatarUrl: null,
      role: "Member"
    },
    {
      id: "member-2-5",
      userId: "user-17",
      name: "Olivia White",
      email: "olivia@example.com",
      title: "Brand Manager",
      avatarUrl: null,
      role: "Member"
    }
  ],
  "team-3": [
    {
      id: "member-3-1",
      userId: "user-18",
      name: "Peter Harris",
      email: "peter@example.com",
      title: "Sales Director",
      avatarUrl: null,
      role: "Admin"
    },
    {
      id: "member-3-2",
      userId: "user-19",
      name: "Quinn Martin",
      email: "quinn@example.com",
      title: "Account Executive",
      avatarUrl: null,
      role: "Member"
    },
    {
      id: "member-3-3",
      userId: "user-20",
      name: "Rachel Clark",
      email: "rachel@example.com",
      title: "Business Development",
      avatarUrl: null,
      role: "Member"
    },
    {
      id: "member-3-4",
      userId: "user-21",
      name: "Sam Lewis",
      email: "sam@example.com",
      title: "Sales Manager",
      avatarUrl: null,
      role: "Admin"
    },
    {
      id: "member-3-5",
      userId: "user-22",
      name: "Tara Walker",
      email: "tara@example.com",
      title: "Inside Sales Rep",
      avatarUrl: null,
      role: "Member"
    },
    {
      id: "member-3-6",
      userId: "user-23",
      name: "Uma Hall",
      email: "uma@example.com",
      title: "Sales Operations",
      avatarUrl: null,
      role: "Member"
    },
    {
      id: "member-3-7",
      userId: "user-24",
      name: "Victor Allen",
      email: "victor@example.com",
      title: "Enterprise Sales",
      avatarUrl: null,
      role: "Member"
    },
    {
      id: "member-3-8",
      userId: "user-25",
      name: "Wendy Young",
      email: "wendy@example.com",
      title: "Customer Success",
      avatarUrl: null,
      role: "Member"
    }
  ],
  "team-4": [
    {
      id: "member-4-1",
      userId: "user-26",
      name: "Xander King",
      email: "xander@example.com",
      title: "Support Manager",
      avatarUrl: null,
      role: "Admin"
    },
    {
      id: "member-4-2",
      userId: "user-27",
      name: "Yara Wright",
      email: "yara@example.com",
      title: "Support Engineer",
      avatarUrl: null,
      role: "Member"
    },
    {
      id: "member-4-3",
      userId: "user-28",
      name: "Zack Scott",
      email: "zack@example.com",
      title: "Technical Support",
      avatarUrl: null,
      role: "Member"
    },
    {
      id: "member-4-4",
      userId: "user-29",
      name: "Amy Green",
      email: "amy@example.com",
      title: "Customer Support",
      avatarUrl: null,
      role: "Member"
    },
    {
      id: "member-4-5",
      userId: "user-30",
      name: "Ben Adams",
      email: "ben@example.com",
      title: "Support Specialist",
      avatarUrl: null,
      role: "Member"
    },
    {
      id: "member-4-6",
      userId: "user-31",
      name: "Cara Baker",
      email: "cara@example.com",
      title: "Support Team Lead",
      avatarUrl: null,
      role: "Admin"
    },
    {
      id: "member-4-7",
      userId: "user-32",
      name: "Dan Nelson",
      email: "dan@example.com",
      title: "Support Engineer",
      avatarUrl: null,
      role: "Member"
    },
    {
      id: "member-4-8",
      userId: "user-33",
      name: "Emma Carter",
      email: "emma@example.com",
      title: "Customer Success",
      avatarUrl: null,
      role: "Member"
    },
    {
      id: "member-4-9",
      userId: "user-34",
      name: "Fred Mitchell",
      email: "fred@example.com",
      title: "Support Analyst",
      avatarUrl: null,
      role: "Member"
    },
    {
      id: "member-4-10",
      userId: "user-35",
      name: "Gina Perez",
      email: "gina@example.com",
      title: "Technical Support",
      avatarUrl: null,
      role: "Member"
    },
    {
      id: "member-4-11",
      userId: "user-36",
      name: "Hank Roberts",
      email: "hank@example.com",
      title: "Support Engineer",
      avatarUrl: null,
      role: "Member"
    },
    {
      id: "member-4-12",
      userId: "user-37",
      name: "Iris Turner",
      email: "iris@example.com",
      title: "Customer Support",
      avatarUrl: null,
      role: "Member"
    },
    {
      id: "member-4-13",
      userId: "user-38",
      name: "James Phillips",
      email: "james@example.com",
      title: "Support Specialist",
      avatarUrl: null,
      role: "Member"
    },
    {
      id: "member-4-14",
      userId: "user-39",
      name: "Kelly Campbell",
      email: "kelly@example.com",
      title: "Technical Support",
      avatarUrl: null,
      role: "Member"
    },
    {
      id: "member-4-15",
      userId: "user-40",
      name: "Larry Parker",
      email: "larry@example.com",
      title: "Support Engineer",
      avatarUrl: null,
      role: "Member"
    }
  ],
  "team-5": [
    {
      id: "member-5-1",
      userId: "user-41",
      name: "Mary Evans",
      email: "mary@example.com",
      title: "Design Director",
      avatarUrl: null,
      role: "Admin"
    },
    {
      id: "member-5-2",
      userId: "user-42",
      name: "Nick Edwards",
      email: "nick@example.com",
      title: "UX Designer",
      avatarUrl: null,
      role: "Member"
    },
    {
      id: "member-5-3",
      userId: "user-43",
      name: "Opal Collins",
      email: "opal@example.com",
      title: "UI Designer",
      avatarUrl: null,
      role: "Member"
    },
    {
      id: "member-5-4",
      userId: "user-44",
      name: "Paul Stewart",
      email: "paul@example.com",
      title: "Product Designer",
      avatarUrl: null,
      role: "Member"
    },
    {
      id: "member-5-5",
      userId: "user-45",
      name: "Quincy Sanchez",
      email: "quincy@example.com",
      title: "Visual Designer",
      avatarUrl: null,
      role: "Member"
    },
    {
      id: "member-5-6",
      userId: "user-46",
      name: "Rosa Morris",
      email: "rosa@example.com",
      title: "UX Researcher",
      avatarUrl: null,
      role: "Admin"
    },
    {
      id: "member-5-7",
      userId: "user-47",
      name: "Steve Rogers",
      email: "steve@example.com",
      title: "Interaction Designer",
      avatarUrl: null,
      role: "Member"
    }
  ]
};
