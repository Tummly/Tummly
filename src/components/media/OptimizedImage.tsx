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
  /** Above-the-fold / LCP — eager + high fetch priority. */
  priority?: boolean;
  /**
   * Start fetch as soon as the element is in the DOM (no IntersectionObserver
   * delay). Does not raise fetch priority — use for below-fold heroes warmed
   * by prefetch, not competing LCP images.
   */
  eager?: boolean;
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
  eager = false,
  className,
  alt = "",
  ...imgProps
}: OptimizedImageProps) {
  const orderedFormats = FORMAT_ORDER.filter((format) => picture.sources[format]);
  const fallbackSrcSet =
    picture.sources.png ??
    picture.sources.jpg ??
    picture.sources.jpeg ??
    picture.sources.webp;
  const loadEager = priority || eager;

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
        srcSet={fallbackSrcSet}
        alt={alt}
        sizes={sizes}
        loading={loadEager ? "eager" : "lazy"}
        fetchPriority={priority ? "high" : undefined}
        decoding={priority ? "sync" : "async"}
        className={cn(className)}
      />
    </picture>
  );
}

export default OptimizedImage;
export type { OptimizedImageProps, PictureOutput };
