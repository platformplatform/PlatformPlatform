import { useEffect, useState } from "react";

export function useCountdown(expireAt: Date) {
  const [secondsRemaining, setSecondsRemaining] = useState(() =>
    Math.max(0, Math.ceil((expireAt.getTime() - Date.now()) / 1000))
  );

  // Reset the countdown when expireAt changes
  useEffect(() => {
    setSecondsRemaining(Math.max(0, Math.ceil((expireAt.getTime() - Date.now()) / 1000)));
  }, [expireAt]);

  useEffect(() => {
    const intervalId = setInterval(() => {
      setSecondsRemaining((prev) => {
        return Math.max(0, prev - 1);
      });
    }, 1000);

    return () => clearInterval(intervalId);
  }, []);

  return secondsRemaining;
}
