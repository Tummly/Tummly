export type PreloadImageResult = "ready" | "timeout" | "error"

export function preloadImage(
  src: string,
  timeoutMs = 2500
): Promise<PreloadImageResult> {
  return new Promise((resolve) => {
    let settled = false
    const finish = (result: PreloadImageResult) => {
      if (settled) {
        return
      }
      settled = true
      globalThis.clearTimeout(timer)
      resolve(result)
    }

    const timer = globalThis.setTimeout(() => {
      finish("timeout")
    }, timeoutMs)

    const image = new Image()
    image.decoding = "async"
    image.onload = () => {
      void image
        .decode()
        .catch(() => undefined)
        .finally(() => {
          finish("ready")
        })
    }
    image.onerror = () => {
      finish("error")
    }
    image.src = src
  })
}
