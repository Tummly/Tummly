import type { ImgHTMLAttributes } from "react"

import { hideBrokenDemoMedia } from "@/lib/productDemo/demoMedia"

/** Demo `<img>` with the neutral fallback: a failed load is hidden and its placeholder box stays. */
export function DemoMediaImage(props: ImgHTMLAttributes<HTMLImageElement>) {
  return <img {...props} onError={hideBrokenDemoMedia} />
}
