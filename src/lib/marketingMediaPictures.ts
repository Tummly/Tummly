import type { PictureOutput } from "@/components/media/OptimizedImage"
import { marketingMediaUrl } from "@/lib/marketingMedia"
import meta from "@/lib/marketingMediaMeta.json" with { type: "json" }

type Format = "avif" | "webp" | "png" | "jpg"

type PictureMeta = {
  kind: "picture"
  w: number
  h: number
  blobDir: string
  baseName: string
  widths: number[]
  formats: Format[]
  fallbackFormat: "png" | "jpg"
}

type SimpleMeta = {
  kind: "simple"
  w: number
  h: number
  blobDir: string
  baseName: string
  widths: number[]
  formats: Format[]
  primaryFormat: Format
  primary?: string
}

type AssetMeta = PictureMeta | SimpleMeta

function asMeta(id: string): AssetMeta {
  const entry = (meta as Record<string, AssetMeta | undefined>)[id]
  if (entry == null) {
    throw new Error(
      `Missing marketing media meta for "${id}". Run: npm run marketing-media:publish`,
    )
  }
  return entry
}

function variantFileName(baseName: string, width: number, format: Format) {
  return `${baseName}-${width}.${format}`
}

function variantUrl(
  blobDir: string,
  baseName: string,
  width: number,
  format: Format,
) {
  return marketingMediaUrl(
    `${blobDir}/${variantFileName(baseName, width, format)}`,
  )
}

function buildSrcset(
  blobDir: string,
  baseName: string,
  widths: number[],
  format: Format,
) {
  return widths
    .map((width) => `${variantUrl(blobDir, baseName, width, format)} ${width}w`)
    .join(", ")
}

/** Build an OptimizedImage-compatible picture from published Blob variants. */
export function remotePicture(id: string): PictureOutput {
  const entry = asMeta(id)
  if (entry.kind !== "picture") {
    throw new Error(`Asset "${id}" is not a picture asset`)
  }

  const sources: Record<string, string> = {}
  for (const format of entry.formats) {
    sources[format] = buildSrcset(
      entry.blobDir,
      entry.baseName,
      entry.widths,
      format,
    )
  }

  const largestWidth = Math.max(...entry.widths)
  const fallbackFormat = entry.fallbackFormat

  return {
    sources,
    img: {
      src: variantUrl(
        entry.blobDir,
        entry.baseName,
        largestWidth,
        fallbackFormat,
      ),
      w: entry.w,
      h: entry.h,
    },
  }
}

/** Absolute URL for a simple (non-srcset) marketing asset on Blob. */
export function remoteSimpleUrl(id: string): string {
  const entry = asMeta(id)
  if (entry.kind !== "simple") {
    throw new Error(`Asset "${id}" is not a simple asset`)
  }

  if (entry.primary != null && entry.primary !== "") {
    return marketingMediaUrl(`${entry.blobDir}/${entry.primary}`)
  }

  const largestWidth = Math.max(...entry.widths)
  return variantUrl(
    entry.blobDir,
    entry.baseName,
    largestWidth,
    entry.primaryFormat,
  )
}
