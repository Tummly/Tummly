/** Max longest edge after resize (px). Matches research target 1920–2560. */
export const HELP_CENTRE_IMAGE_MAX_EDGE_PX = 2048

/** JPEG / WebP encode quality (0–1). */
export const HELP_CENTRE_IMAGE_QUALITY = 0.8

const COMPRESSIBLE_TYPES = new Set([
  "image/jpeg",
  "image/png",
  "image/webp",
])

export function isHelpCentreCompressibleImage(file: File): boolean {
  return COMPRESSIBLE_TYPES.has(file.type)
}

export function scaleImageDimensions(
  width: number,
  height: number,
  maxEdgePx: number = HELP_CENTRE_IMAGE_MAX_EDGE_PX
): { width: number; height: number } {
  const longest = Math.max(width, height)
  if (longest <= maxEdgePx) {
    return { width, height }
  }

  const scale = maxEdgePx / longest
  return {
    width: Math.max(1, Math.round(width * scale)),
    height: Math.max(1, Math.round(height * scale)),
  }
}

export function replaceFileExtension(fileName: string, extension: string): string {
  const normalized =
    extension.startsWith(".") ? extension : `.${extension}`
  const dot = fileName.lastIndexOf(".")
  if (dot <= 0) {
    return `${fileName}${normalized}`
  }
  return `${fileName.slice(0, dot)}${normalized}`
}

/**
 * Output format for compressible attachments.
 * PNG → JPEG (screenshots shrink a lot; alpha flattened on white).
 * JPEG / WebP keep their type.
 */
export function helpCentreImageOutputFormat(inputType: string): {
  type: "image/jpeg" | "image/webp"
  extension: ".jpg" | ".webp"
  quality: number
} {
  if (inputType === "image/webp") {
    return {
      type: "image/webp",
      extension: ".webp",
      quality: HELP_CENTRE_IMAGE_QUALITY,
    }
  }

  return {
    type: "image/jpeg",
    extension: ".jpg",
    quality: HELP_CENTRE_IMAGE_QUALITY,
  }
}

function canvasToBlob(
  canvas: HTMLCanvasElement,
  type: string,
  quality: number
): Promise<Blob | null> {
  return new Promise((resolve) => {
    canvas.toBlob((blob) => resolve(blob), type, quality)
  })
}

/**
 * Resize and re-encode a Help Centre image attachment in the browser.
 * GIF and PDF are returned unchanged. On failure or if the result is not
 * smaller, the original file is returned.
 */
export async function compressHelpCentreImage(file: File): Promise<File> {
  if (!isHelpCentreCompressibleImage(file)) {
    return file
  }

  const output = helpCentreImageOutputFormat(file.type)

  try {
    const bitmap = await createImageBitmap(file)
    try {
      const { width, height } = scaleImageDimensions(
        bitmap.width,
        bitmap.height
      )

      const canvas = document.createElement("canvas")
      canvas.width = width
      canvas.height = height

      const context = canvas.getContext("2d")
      if (!context) {
        return file
      }

      if (output.type === "image/jpeg") {
        context.fillStyle = "#ffffff"
        context.fillRect(0, 0, width, height)
      }

      context.drawImage(bitmap, 0, 0, width, height)

      const blob = await canvasToBlob(canvas, output.type, output.quality)
      if (!blob || blob.size >= file.size) {
        return file
      }

      return new File(
        [blob],
        replaceFileExtension(file.name, output.extension),
        {
          type: output.type,
          lastModified: Date.now(),
        }
      )
    } finally {
      bitmap.close()
    }
  } catch {
    return file
  }
}

export async function compressHelpCentreAttachments(
  files: File[]
): Promise<File[]> {
  return Promise.all(files.map((file) => compressHelpCentreImage(file)))
}
