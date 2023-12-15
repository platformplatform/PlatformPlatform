import { useEffect, useState } from "react";
import Confetti, { type ConfettiConfig } from "react-dom-confetti";

const config: ConfettiConfig = {
  angle: 90,
  spread: 360,
  startVelocity: 28,
  elementCount: 170,
  dragFriction: 0.12,
  duration: 3000,
  stagger: 3,
  width: "10px",
  height: "10px",
  //perspective: "500px",
  colors: ["#a864fd", "#29cdff", "#78ff44", "#ff718d", "#fdff6a"],
};

type PageProps = {
  params: Record<string, string>;
};

export default function CreatedTenantSuccessPage({}: PageProps) {
  const [confetti, setConfetti] = useState(false);

  useEffect(() => {
    setConfetti(true);
  }, []);

  return (
    <div className="items-center flex flex-col justify-center h-full">
      <div className="p-8 bg-gray-800 text-white rounded-xl shadow-md text-center gap-4 flex flex-col">
        <h1 className="text-2xl">Success!</h1>
        <p>Your account has been created.</p>
        <div className="self-center absolute top-10">
          <Confetti active={confetti} config={config} />
        </div>
      </div>
    </div>
  );
}
