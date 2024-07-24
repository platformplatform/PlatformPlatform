import avatarUrl from "./images/avatar.png";

const AvatarMenuItem: React.FC = () => {
  const name = "Mary Doe";
  const title = "DevOps Engineer";

  return (
    <div className="flex flex-row items-center gap-2">
      <div>
        <img src={avatarUrl} alt={name} className="mr-2 w-10 h-10 rounded-full bg-transparent" />
      </div>
      <div className="flex flex-col">
        <h2>{name}</h2>
        <p className=" text-slate-500 text-sm font-normal">{title}</p>
      </div>
    </div>
  );
};

export default AvatarMenuItem;
