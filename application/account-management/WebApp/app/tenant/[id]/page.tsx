interface PageProps {
  params: Record<string, string>;
}

export default function Page({ params }: Readonly<PageProps>) {
  return (
    <div>
      <h1>
        Show account id: &quot;{params.id}&quot;
      </h1>
    </div>
  );
}
