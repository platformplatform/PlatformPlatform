import type { LucideProps } from "lucide-react";

import { forwardRef } from "react";

export const TeamsIcon = forwardRef<SVGSVGElement, LucideProps>(
  ({ size = 24, strokeWidth = 2, className, ...rest }, ref) => (
    <svg
      ref={ref}
      xmlns="http://www.w3.org/2000/svg"
      width={size}
      height={size}
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth={strokeWidth}
      strokeLinecap="round"
      strokeLinejoin="round"
      className={className}
      {...rest}
    >
      <circle cx="12" cy="7" r="3" />
      <circle cx="5" cy="8.5" r="2" />
      <circle cx="19" cy="8.5" r="2" />
      <path d="M6 21v-3a3 3 0 0 1 3-3h6a3 3 0 0 1 3 3v3" />
      <path d="M2 20v-2a2 2 0 0 1 2-2h2" />
      <path d="M22 20v-2a2 2 0 0 0-2-2h-2" />
    </svg>
  )
);

TeamsIcon.displayName = "TeamsIcon";
