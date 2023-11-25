type PageProps = {
  params: Record<string, string>;
};

export default function LoadingPage({ params }: PageProps) {
  return (
    <div className="items-center flex flex-col justify-center h-full">
      <div className="p-8 bg-gray-800 text-white rounded-xl shadow-md text-center gap-4 flex flex-col animate-ping"></div>
    </div>
  );
}
