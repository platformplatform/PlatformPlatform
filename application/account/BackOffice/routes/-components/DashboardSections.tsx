import { DashboardKpiCards } from "./DashboardKpiCards";
import { DashboardTrendsSection } from "./DashboardTrendsSection";

export function DashboardSections() {
  return (
    <div className="flex flex-col gap-6">
      <DashboardKpiCards />
      <DashboardTrendsSection />
    </div>
  );
}
