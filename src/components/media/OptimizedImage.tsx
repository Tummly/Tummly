import type { ImgHTMLAttributes } from "react";

import { cn } from "@/lib/utils";

type PictureOutput = {
  sources: Record<string, string>;
  img: {
    src: string;
    w: number;
    h: number;
  };
};

type OptimizedImageProps = Omit<
  ImgHTMLAttributes<HTMLImageElement>,
  "src" | "srcSet" | "sizes"
> & {
  picture: PictureOutput;
  sizes: string;
  priority?: boolean;
};

const FORMAT_MIME: Record<string, string> = {
  avif: "image/avif",
  webp: "image/webp",
  png: "image/png",
  jpg: "image/jpeg",
  jpeg: "image/jpeg",
};

const FORMAT_ORDER = ["avif", "webp", "png", "jpg", "jpeg"] as const;

function OptimizedImage({
  picture,
  sizes,
  priority = false,
  className,
  alt = "",
  ...imgProps
}: OptimizedImageProps) {
  const orderedFormats = FORMAT_ORDER.filter((format) => picture.sources[format]);

  return (
    <picture>
      {orderedFormats.map((format) => (
        <source
          key={format}
          type={FORMAT_MIME[format]}
          srcSet={picture.sources[format]}
          sizes={sizes}
        />
      ))}
      <img
        {...imgProps}
        src={picture.img.src}
        alt={alt}
        sizes={sizes}
        loading={priority ? "eager" : "lazy"}
        fetchPriority={priority ? "high" : undefined}
        decoding={priority ? "sync" : "async"}
        className={cn(className)}
      />
    </picture>
  );
}

export default OptimizedImage;
export type { OptimizedImageProps, PictureOutput };
