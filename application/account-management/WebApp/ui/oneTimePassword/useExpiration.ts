"use client";
import { useEffect, useState } from "react";
import { VerificationExpirationError } from "./VerificationExpirationError";

export function useExpirationTimeout(expiresAt: Date) {
  const [expiresInSeconds, setExpiresInSeconds] = useState(
    getExpiresInSeconds(expiresAt)
  );

  useEffect(() => {
    const interval = setInterval(() => {
      setExpiresInSeconds(getExpiresInSeconds(expiresAt));
    }, 1000);
    return () => clearInterval(interval);
  }, [expiresAt]);

  if (expiresAt.getTime() < Date.now())
    throw new VerificationExpirationError();

  return {
    expiresInSeconds,
    expiresInString: getExpiresInString(expiresInSeconds),
    isExpired: expiresInSeconds === 0,
  };
}

export function getExpiresInSeconds(expiresAt: Date) {
  return Math.max(0, Math.floor((expiresAt.getTime() - Date.now()) / 1000));
}

export function getExpiresInString(seconds: number) {
  const minutes = Math.floor(seconds / 60);
  const remainingSeconds = seconds % 60;
  return `${minutes}:${remainingSeconds.toString().padStart(2, "0")}`;
}
