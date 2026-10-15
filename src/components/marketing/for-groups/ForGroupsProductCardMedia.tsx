import { useEffect, useRef, useState } from "react"

import { cn } from "@/lib/utils"

type ForGroupsProductCardMediaProps = {
  videoSrc: string
  posterSrc: string
  className?: string
}

/**
 * Frame 5564 media plane — looping muted video over a Figma poster still.
 * Poster stays visible when the video is missing or fails to load.
 */
export function ForGroupsProductCardMedia({
  videoSrc,
  posterSrc,
  className,
}: ForGroupsProductCardMediaProps) {
  const rootRef = useRef<HTMLDivElement>(null)
  const videoRef = useRef<HTMLVideoElement>(null)
  const [shouldLoad, setShouldLoad] = useState(false)
  const [inView, setInView] = useState(false)
  const [videoFailed, setVideoFailed] = useState(false)

  useEffect(() => {
    const root = rootRef.current
    if (root == null || typeof IntersectionObserver === "undefined") {
      setShouldLoad(true)
      setInView(true)
      return
    }

    const observer = new IntersectionObserver(
      ([entry]) => {
        const visible = entry?.isIntersecting === true
        setInView(visible)
        if (visible) {
          setShouldLoad(true)
        }
      },
      { root: null, rootMargin: "0px", threshold: 0.2 },
    )

    observer.observe(root)
    return () => observer.disconnect()
  }, [])

  useEffect(() => {
    const el = videoRef.current
    if (el == null || !shouldLoad || videoFailed) {
      return
    }

    if (inView) {
      if (el.readyState < HTMLMediaElement.HAVE_CURRENT_DATA) {
        el.load()
      }
      void el.play().catch(() => {
        /* Autoplay can still be blocked. */
      })
    } else {
      el.pause()
    }
  }, [inView, shouldLoad, videoFailed, videoSrc])

  return (
    <div
      ref={rootRef}
      className={cn("absolute inset-0 overflow-hidden", className)}
      aria-hidden
    >
      <img
        src={posterSrc}
        alt=""
        width={365}
        height={496}
        className="absolute inset-0 size-full object-cover"
        draggable={false}
        decoding="async"
      />
      {shouldLoad && !videoFailed ? (
        <video
          key={videoSrc}
          ref={videoRef}
          className="absolute inset-0 size-full object-cover"
          muted
          loop
          playsInline
          preload="metadata"
          poster={posterSrc}
          onError={() => setVideoFailed(true)}
        >
          <source src={videoSrc} type="video/mp4" />
        </video>
      ) : null}
    </div>
  )
}
