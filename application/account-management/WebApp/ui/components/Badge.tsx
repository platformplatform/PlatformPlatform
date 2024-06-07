import React from "react";

interface BadgeProps {
  children: React.ReactNode;
  variant?: "primary" | "secondary" | "success" | "danger" | "warning" | "info";
}

const Badge: React.FC<BadgeProps> = ({ children, variant = "primary" }) => {
  const variantClasses = {
    primary: "border-indigo-900 text-indigo-900",
    secondary: "border-gray-200 border-1 text-slate-700 text-xs font-medium",
    success: "border-green-500 text-green-500",
    danger: "border-red-500 text-red-500",
    warning: "border-yellow-500 text-yellow-500",
    info: "border-teal-500 text-teal-500",
  };

  return (
    <div className={`px-2 py-0.5 rounded-full border inline-flex justify-start items-center ${variantClasses[variant]}`}>
      <div className="text-xs font-medium">{children}</div>
    </div>
  );
};

export default Badge;
