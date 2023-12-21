type PageProps = {
  params: Record<string, string>;
};

export default function Page({ params }: Readonly<PageProps>) {
  return (
    <div>
      <h1>Show account id: "{params.id}"</h1>
    </div>
  );
}
