import { useEffect, useState } from "react";

export function useElementSize(element: HTMLElement) {
  const [height, setHeight] = useState(0);
  const [width, setWidth] = useState(0);
  useEffect(() => {
    function handleResize() {
      setHeight(element.clientHeight);
      setWidth(element.clientWidth);
    }
    handleResize();
    window.addEventListener("resize", handleResize);
    return () => {
      // Clean Up
      window.removeEventListener("resize", handleResize);
    };
  });
  return { height, width };
}
