export interface TenantUser {
  userId: string;
  name: string;
  email: string;
  title: string;
  avatarUrl: string | null;
}

export const mockTenantUsers: TenantUser[] = [
  { userId: "user-1", name: "John Doe", email: "john@example.com", title: "Senior Developer", avatarUrl: null },
  { userId: "user-2", name: "Jane Smith", email: "jane@example.com", title: "Product Manager", avatarUrl: null },
  { userId: "user-3", name: "Bob Wilson", email: "bob@example.com", title: "Backend Engineer", avatarUrl: null },
  { userId: "user-4", name: "Alice Johnson", email: "alice@example.com", title: "Frontend Engineer", avatarUrl: null },
  { userId: "user-5", name: "Charlie Brown", email: "charlie@example.com", title: "DevOps Engineer", avatarUrl: null },
  { userId: "user-6", name: "Diana Prince", email: "diana@example.com", title: "QA Engineer", avatarUrl: null },
  { userId: "user-7", name: "Eve Anderson", email: "eve@example.com", title: "UX Designer", avatarUrl: null },
  { userId: "user-8", name: "Frank Miller", email: "frank@example.com", title: "Tech Lead", avatarUrl: null },
  { userId: "user-9", name: "Grace Lee", email: "grace@example.com", title: "Software Engineer", avatarUrl: null },
  { userId: "user-10", name: "Henry Davis", email: "henry@example.com", title: "Data Engineer", avatarUrl: null },
  { userId: "user-11", name: "Ivy Chen", email: "ivy@example.com", title: "Mobile Developer", avatarUrl: null },
  { userId: "user-12", name: "Jack Taylor", email: "jack@example.com", title: "Security Engineer", avatarUrl: null },
  { userId: "user-13", name: "Kate Martinez", email: "kate@example.com", title: "Marketing Director", avatarUrl: null },
  { userId: "user-14", name: "Leo Garcia", email: "leo@example.com", title: "Content Strategist", avatarUrl: null },
  {
    userId: "user-15",
    name: "Mia Rodriguez",
    email: "mia@example.com",
    title: "Social Media Manager",
    avatarUrl: null
  },
  { userId: "user-16", name: "Noah Thompson", email: "noah@example.com", title: "SEO Specialist", avatarUrl: null },
  { userId: "user-17", name: "Olivia White", email: "olivia@example.com", title: "Brand Manager", avatarUrl: null },
  { userId: "user-18", name: "Peter Harris", email: "peter@example.com", title: "Sales Director", avatarUrl: null },
  { userId: "user-19", name: "Quinn Martin", email: "quinn@example.com", title: "Account Executive", avatarUrl: null },
  {
    userId: "user-20",
    name: "Rachel Clark",
    email: "rachel@example.com",
    title: "Business Development",
    avatarUrl: null
  },
  { userId: "user-21", name: "Sam Lewis", email: "sam@example.com", title: "Sales Manager", avatarUrl: null },
  { userId: "user-22", name: "Tara Walker", email: "tara@example.com", title: "Inside Sales Rep", avatarUrl: null },
  { userId: "user-23", name: "Uma Hall", email: "uma@example.com", title: "Sales Operations", avatarUrl: null },
  { userId: "user-24", name: "Victor Allen", email: "victor@example.com", title: "Enterprise Sales", avatarUrl: null },
  { userId: "user-25", name: "Wendy Young", email: "wendy@example.com", title: "Customer Success", avatarUrl: null },
  { userId: "user-26", name: "Xander King", email: "xander@example.com", title: "Support Manager", avatarUrl: null },
  { userId: "user-27", name: "Yara Wright", email: "yara@example.com", title: "Support Engineer", avatarUrl: null },
  { userId: "user-28", name: "Zack Scott", email: "zack@example.com", title: "Technical Support", avatarUrl: null },
  { userId: "user-29", name: "Amy Green", email: "amy@example.com", title: "Customer Support", avatarUrl: null },
  { userId: "user-30", name: "Ben Adams", email: "ben@example.com", title: "Support Specialist", avatarUrl: null },
  { userId: "user-31", name: "Cara Baker", email: "cara@example.com", title: "Support Team Lead", avatarUrl: null },
  { userId: "user-32", name: "Dan Nelson", email: "dan@example.com", title: "Support Engineer", avatarUrl: null },
  { userId: "user-33", name: "Emma Carter", email: "emma@example.com", title: "Customer Success", avatarUrl: null },
  { userId: "user-34", name: "Fred Mitchell", email: "fred@example.com", title: "Support Analyst", avatarUrl: null },
  { userId: "user-35", name: "Gina Perez", email: "gina@example.com", title: "Technical Support", avatarUrl: null },
  { userId: "user-36", name: "Hank Roberts", email: "hank@example.com", title: "Support Engineer", avatarUrl: null },
  { userId: "user-37", name: "Iris Turner", email: "iris@example.com", title: "Customer Support", avatarUrl: null },
  {
    userId: "user-38",
    name: "James Phillips",
    email: "james@example.com",
    title: "Support Specialist",
    avatarUrl: null
  },
  {
    userId: "user-39",
    name: "Kelly Campbell",
    email: "kelly@example.com",
    title: "Technical Support",
    avatarUrl: null
  },
  { userId: "user-40", name: "Larry Parker", email: "larry@example.com", title: "Support Engineer", avatarUrl: null },
  { userId: "user-41", name: "Mary Evans", email: "mary@example.com", title: "Design Director", avatarUrl: null },
  { userId: "user-42", name: "Nick Edwards", email: "nick@example.com", title: "UX Designer", avatarUrl: null },
  { userId: "user-43", name: "Opal Collins", email: "opal@example.com", title: "UI Designer", avatarUrl: null },
  { userId: "user-44", name: "Paul Stewart", email: "paul@example.com", title: "Product Designer", avatarUrl: null },
  { userId: "user-45", name: "Quincy Sanchez", email: "quincy@example.com", title: "Visual Designer", avatarUrl: null },
  { userId: "user-46", name: "Rosa Morris", email: "rosa@example.com", title: "UX Researcher", avatarUrl: null },
  {
    userId: "user-47",
    name: "Steve Rogers",
    email: "steve@example.com",
    title: "Interaction Designer",
    avatarUrl: null
  },
  { userId: "user-48", name: "Tina Brown", email: "tina@example.com", title: "Graphic Designer", avatarUrl: null },
  { userId: "user-49", name: "Uma Singh", email: "uma.singh@example.com", title: "Product Owner", avatarUrl: null },
  {
    userId: "user-50",
    name: "Victor Chen",
    email: "victor.chen@example.com",
    title: "Solutions Architect",
    avatarUrl: null
  }
];
