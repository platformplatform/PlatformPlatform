interface LayoutProps {
  children: React.ReactNode;
  params: Record<string, string>;
}

export default function LoginLayout({ children }: Readonly<LayoutProps>) {
  return children;
}
