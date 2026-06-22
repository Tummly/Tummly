import type { PictureOutput } from "@/components/media/OptimizedImage";

function largestSrcFromSrcset(srcset: string): string {
  const entries = srcset.split(",").map((entry) => entry.trim());
  const last = entries[entries.length - 1] ?? entries[0] ?? "";
  return last.split(/\s+/)[0] ?? "";
}

/** CSS `image-set()` value for decorative `background-image` usage. */
export function pictureToImageSet(picture: PictureOutput): string {
  const parts: string[] = [];

  if (picture.sources.avif) {
    parts.push(
      `url('${largestSrcFromSrcset(picture.sources.avif)}') type('image/avif')`,
    );
  }

  if (picture.sources.webp) {
    parts.push(
      `url('${largestSrcFromSrcset(picture.sources.webp)}') type('image/webp')`,
    );
  }

  if (picture.sources.png) {
    parts.push(
      `url('${largestSrcFromSrcset(picture.sources.png)}') type('image/png')`,
    );
  } else if (picture.sources.jpg || picture.sources.jpeg) {
    const jpgSrcset = picture.sources.jpg ?? picture.sources.jpeg;
    parts.push(
      `url('${largestSrcFromSrcset(jpgSrcset)}') type('image/jpeg')`,
    );
  } else {
    parts.push(`url('${picture.img.src}') type('image/png')`);
  }

  return `image-set(${parts.join(", ")}), url('${picture.img.src}')`;
}
