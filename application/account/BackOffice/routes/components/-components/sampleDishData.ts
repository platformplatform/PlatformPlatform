export type Cuisine = "Italian" | "Japanese" | "Mexican" | "French" | "Indian" | "Thai";

export type Difficulty = "Easy" | "Medium" | "Hard";

export interface SampleDish {
  id: number;
  name: string;
  description: string;
  cuisine: Cuisine;
  cookTime: number;
  difficulty: Difficulty;
  addedAt: string;
}

export const sampleDishes: SampleDish[] = [
  {
    id: 1,
    name: "Spaghetti carbonara",
    description: "Creamy pasta with pancetta, egg yolks, and pecorino",
    cuisine: "Italian",
    cookTime: 25,
    difficulty: "Easy",
    addedAt: "2025-11-12"
  },
  {
    id: 2,
    name: "Chicken katsu",
    description: "Panko-crusted chicken cutlet with tonkatsu sauce",
    cuisine: "Japanese",
    cookTime: 30,
    difficulty: "Easy",
    addedAt: "2025-12-03"
  },
  {
    id: 3,
    name: "Pizza margherita",
    description: "Wood-fired pizza with tomato, mozzarella, and basil",
    cuisine: "Italian",
    cookTime: 90,
    difficulty: "Medium",
    addedAt: "2026-01-15"
  },
  {
    id: 4,
    name: "Ratatouille",
    description: "Slow-cooked stew of eggplant, zucchini, and tomatoes",
    cuisine: "French",
    cookTime: 80,
    difficulty: "Easy",
    addedAt: "2026-01-22"
  },
  {
    id: 5,
    name: "Butter chicken",
    description: "Tandoori chicken in a silky tomato and cream sauce",
    cuisine: "Indian",
    cookTime: 45,
    difficulty: "Easy",
    addedAt: "2026-02-01"
  },
  {
    id: 6,
    name: "Tonkotsu ramen",
    description: "Rich pork bone broth with springy noodles and chashu",
    cuisine: "Japanese",
    cookTime: 720,
    difficulty: "Hard",
    addedAt: "2026-02-10"
  },
  {
    id: 7,
    name: "Tacos al pastor",
    description: "Marinated pork tacos with pineapple and cilantro",
    cuisine: "Mexican",
    cookTime: 180,
    difficulty: "Medium",
    addedAt: "2026-02-18"
  },
  {
    id: 8,
    name: "Pad thai",
    description: "Stir-fried rice noodles with shrimp, peanuts, and tamarind",
    cuisine: "Thai",
    cookTime: 35,
    difficulty: "Easy",
    addedAt: "2026-03-01"
  },
  {
    id: 9,
    name: "Risotto alla milanese",
    description: "Creamy saffron rice with parmesan and bone marrow",
    cuisine: "Italian",
    cookTime: 40,
    difficulty: "Medium",
    addedAt: "2026-03-10"
  },
  {
    id: 10,
    name: "Lamb biryani",
    description: "Layered basmati rice with slow-braised spiced lamb",
    cuisine: "Indian",
    cookTime: 120,
    difficulty: "Hard",
    addedAt: "2026-03-15"
  },
  {
    id: 11,
    name: "Boeuf bourguignon",
    description: "Beef braised in red wine with pearl onions and mushrooms",
    cuisine: "French",
    cookTime: 210,
    difficulty: "Medium",
    addedAt: "2026-03-20"
  },
  {
    id: 12,
    name: "Mole poblano",
    description: "Chicken in a complex chili, chocolate, and spice sauce",
    cuisine: "Mexican",
    cookTime: 240,
    difficulty: "Hard",
    addedAt: "2026-03-28"
  }
];

export const pageSize = 5;

export const difficultyVariant: Record<Difficulty, "default" | "secondary" | "outline"> = {
  Easy: "default",
  Medium: "secondary",
  Hard: "outline"
};

export function formatCookTime(minutes: number): string {
  if (minutes < 60) {
    return `${minutes} min`;
  }
  const hours = Math.floor(minutes / 60);
  const remainingMinutes = minutes % 60;
  if (remainingMinutes === 0) {
    return `${hours} h`;
  }
  return `${hours} h ${remainingMinutes} min`;
}
