import { useCallback, useEffect, useRef, useState } from "react";
import type { CSSProperties, ImgHTMLAttributes } from "react";

export interface ImageLoaderOptions {
  src: string;
  width: number;
  quality: number;
}

export type ImageLoader = (options: ImageLoaderOptions) => string;

export interface ImageProps extends ImgHTMLAttributes<HTMLImageElement> {
  src: string;
  alt: string;
  height: number;
  width: number;
  priority?: boolean;
  quality?: number;
  loader?: ImageLoader;
  blurDataURL?: string;
}

export function Image(props: Readonly<ImageProps>) {
  const imageRef = useRef<HTMLImageElement>(null);
  const [isLoaded, setIsLoaded] = useState(false);
  const { src, alt, height, width, priority, quality, loader, style, blurDataURL, className, ...imageProps } = props;
  const imageLoader = loader ?? defaultLoader;
  const imageUrl = imageLoader({ src, width, quality: quality ?? 75 });

  const handleLoad = useCallback(() => setIsLoaded(true), []);

  useEffect(() => {
    const image = imageRef.current;
    if (!image || isLoaded)
      return;

    if (image.complete && image.naturalWidth > 0) {
      handleLoad();
      return;
    }

    image.addEventListener("load", handleLoad);
    return () => image.removeEventListener("load", handleLoad);
  }, [handleLoad, isLoaded]);

  const image = (
    <img
      ref={imageRef}
      src={imageUrl}
      width={width}
      height={height}
      decoding={priority ? undefined : "async"}
      loading={priority ? undefined : "lazy"}
      alt={alt}
      style={{
        ...style,
        color: "transparent", // Hide the broken image icon
        maxWidth: "100%", // Make sure the image doesn't overflow its container
        height: "auto",
        objectFit: "cover",
      }}
      className={className}
      {...imageProps}
    />
  );

  if (blurDataURL == null)
    return image;

  const blurBackground: CSSProperties = !isLoaded
    ? {
        filter: "blur(15px)",
        backgroundImage: `url(${blurDataURL})`,
        backgroundSize: "100% 100%",
      }
    : {
        filter: "blur(0px)",
        transition: "filter 0.3s",
      };

  return (
    <div
      style={{
        position: "relative",
        color: "transparent",
        width,
        maxWidth: "100%",
        aspectRatio: `${width} / ${height}`,
        ...blurBackground,
      }}
      className={className}
    >
      {image}
      <div
        style={{
          position: "absolute",
          top: 0,
          left: 0,
          bottom: 0,
          right: 0,
          display: "block",
          alignItems: "center",
          justifyContent: "center",
          color: "transparent",
        }}
      />
    </div>
  );
}

function defaultLoader({ src }: ImageLoaderOptions) {
  if (src.startsWith("/"))
    return import.meta.env.CDN_URL + src;
  return src;
}
