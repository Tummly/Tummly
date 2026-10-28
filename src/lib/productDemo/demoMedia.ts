import type { SyntheticEvent } from "react"

/** Hides a demo image that failed to load, so no broken-image icon or alt text shows. */
export function hideBrokenDemoMedia(event: SyntheticEvent<HTMLImageElement>) {
  event.currentTarget.hidden = true
}
