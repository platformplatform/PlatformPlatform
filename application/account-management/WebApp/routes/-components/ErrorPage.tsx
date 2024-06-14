import type { ErrorComponentProps } from "@tanstack/react-router";
import { useEffect } from "react";

export function ErrorPage({ error, reset }: ErrorComponentProps) {
  useEffect(() => {
    // Log the error to an error reporting service
    console.error(error);
  }, [error]);

  return (
    <div>
      <h2>Something went wrong!</h2>
      <button
        type="button"
        onClick={
          // Attempt to recover by trying to re-render the segment
          () => reset()
        }
      >
        Try again
      </button>
      <div>
        <h3>Error details:</h3>
        <pre>{error.message}</pre>
      </div>
    </div>
  );
}
