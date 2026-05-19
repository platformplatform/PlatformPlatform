import { t } from "@lingui/core/macro";
import { AreaChartIcon, BarChart3Icon, LineChartIcon, PieChartIcon, RadarIcon } from "lucide-react";

export function useRecipeGroups() {
  return [
    {
      cuisine: t`Italian`,
      recipes: [
        { value: "carbonara", label: t`Spaghetti carbonara` },
        { value: "margherita", label: t`Pizza margherita` },
        { value: "risotto", label: t`Risotto alla milanese` },
        { value: "lasagna", label: t`Lasagna bolognese` },
        { value: "osso-buco", label: t`Osso buco` },
        { value: "tiramisu", label: t`Tiramisu` }
      ]
    },
    {
      cuisine: t`Japanese`,
      recipes: [
        { value: "ramen", label: t`Tonkotsu ramen` },
        { value: "sushi", label: t`Nigiri sushi` },
        { value: "katsu", label: t`Chicken katsu` },
        { value: "tempura", label: t`Vegetable tempura` },
        { value: "okonomiyaki", label: t`Okonomiyaki` },
        { value: "yakitori", label: t`Yakitori skewers` }
      ]
    },
    {
      cuisine: t`Mexican`,
      recipes: [
        { value: "tacos", label: t`Tacos al pastor` },
        { value: "mole", label: t`Mole poblano` },
        { value: "enchiladas", label: t`Enchiladas verdes` },
        { value: "chiles-rellenos", label: t`Chiles rellenos` },
        { value: "pozole", label: t`Pozole rojo` }
      ]
    },
    {
      cuisine: t`French`,
      recipes: [
        { value: "boeuf-bourguignon", label: t`Boeuf bourguignon` },
        { value: "ratatouille", label: t`Ratatouille` },
        { value: "coq-au-vin", label: t`Coq au vin` },
        { value: "creme-brulee", label: t`Creme brulee` }
      ]
    },
    {
      cuisine: t`Indian`,
      recipes: [
        { value: "butter-chicken", label: t`Butter chicken` },
        { value: "biryani", label: t`Lamb biryani` },
        { value: "palak-paneer", label: t`Palak paneer` },
        { value: "dosa", label: t`Masala dosa` },
        { value: "rogan-josh", label: t`Rogan josh` }
      ]
    },
    {
      cuisine: t`Thai`,
      recipes: [
        { value: "pad-thai", label: t`Pad thai` },
        { value: "green-curry", label: t`Green curry` },
        { value: "tom-yum", label: t`Tom yum soup` },
        { value: "som-tam", label: t`Som tam salad` }
      ]
    }
  ];
}

export function useChartItems() {
  return [
    { id: "bar", label: t`Bar chart`, icon: <BarChart3Icon /> },
    { id: "line", label: t`Line chart`, icon: <LineChartIcon /> },
    { id: "pie", label: t`Pie chart`, icon: <PieChartIcon /> },
    { id: "area", label: t`Area chart`, icon: <AreaChartIcon /> },
    { id: "radar", label: t`Radar chart`, icon: <RadarIcon /> }
  ];
}

export function useChartSelectItems() {
  return [
    { value: "bar", label: t`Bar chart`, icon: <BarChart3Icon /> },
    { value: "line", label: t`Line chart`, icon: <LineChartIcon /> },
    { value: "pie", label: t`Pie chart`, icon: <PieChartIcon /> }
  ];
}
