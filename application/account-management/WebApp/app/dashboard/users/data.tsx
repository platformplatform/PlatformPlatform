import avatarUrl from "../images/avatar.png";

export interface User {
  name: string;
  email: string;
  added: Date;
  lastSeen: Date;
  role: string;
  profilePicture?: string;
}

export const rows: User[] = [
  {
    name: "John Doe",
    email: "john@example.com",
    added: new Date(),
    lastSeen: new Date(),
    role: "Admin",
    profilePicture: avatarUrl,
  },
  {
    name: "Jane Doe",
    email: "jane@example.com",
    added: new Date(),
    lastSeen: new Date(),
    role: "User",
    profilePicture: avatarUrl,
  },
  {
    name: "Alice Doe",
    email: "alice@example.com",
    added: new Date(),
    lastSeen: new Date(),
    role: "Admin",
    profilePicture: avatarUrl,
  },
  {
    name: "Bob Doe",
    email: "bob@example.com",
    added: new Date(),
    lastSeen: new Date(),
    role: "User",
  },
  {
    name: "James Doe",
    email: "james@example.com",
    added: new Date(),
    lastSeen: new Date(),
    role: "User",
    profilePicture: avatarUrl,
  },
  {
    name: "Mary Doe",
    email: "mary@example.com",
    added: new Date(),
    lastSeen: new Date(),
    role: "User",
    profilePicture: avatarUrl,
  },
];
