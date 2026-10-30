import { useEffect, useRef, useState, type VideoHTMLAttributes } from "react"

import { cn } from "@/lib/utils"

type MarketingLoopVideoProps = {
  src: string
  className?: string
  videoClassName?: string
  /**
   * How far outside the viewport the video mounts. Default `200px 0px`.
   * Pair a wider margin with `preload="auto"` so the file downloads before the
   * card scrolls in (with the default `preload="none"` nothing loads until play).
   */
  loadRootMargin?: string
} & Omit<
  VideoHTMLAttributes<HTMLVideoElement>,
  "src" | "muted" | "controls" | "autoPlay" | "className" | "loop" | "playsInline"
>

function videoSourceType(src: string): string | undefined {
  const path = src.split("?")[0]?.toLowerCase() ?? ""
  if (path.endsWith(".webm")) {
    return "video/webm"
  }
  if (path.endsWith(".mp4")) {
    return "video/mp4"
  }
  return undefined
}

/**
 * Lightweight muted marketing loop. Loads only near the viewport, plays only
 * while visible, and pauses (no decode work) when the user scrolls away.
 */
export function MarketingLoopVideo({
  src,
  className,
  videoClassName,
  loadRootMargin = "200px 0px",
  ...videoProps
}: MarketingLoopVideoProps) {
  const rootRef = useRef<HTMLDivElement>(null)
  const videoRef = useRef<HTMLVideoElement>(null)
  const [shouldLoad, setShouldLoad] = useState(false)
  const [inView, setInView] = useState(false)
  const [failed, setFailed] = useState(false)

  useEffect(() => {
    const root = rootRef.current
    if (root == null || typeof IntersectionObserver === "undefined") {
      setShouldLoad(true)
      setInView(true)
      return
    }

    // Prefetch when the card is near the viewport.
    const loadObserver = new IntersectionObserver(
      ([entry]) => {
        if (entry?.isIntersecting === true) {
          setShouldLoad(true)
        }
      },
      { root: null, rootMargin: loadRootMargin, threshold: 0 },
    )

    // Play as soon as any part is on screen. A 0.3 gate left For Groups product
    // cards paused while the section was only partly visible.
    const playObserver = new IntersectionObserver(
      ([entry]) => {
        setInView(entry?.isIntersecting === true)
      },
      { root: null, rootMargin: "80px 0px", threshold: 0 },
    )

    loadObserver.observe(root)
    playObserver.observe(root)
    return () => {
      loadObserver.disconnect()
      playObserver.disconnect()
    }
  }, [loadRootMargin])

  useEffect(() => {
    const el = videoRef.current
    if (el == null || !shouldLoad || failed) {
      return
    }

    el.muted = true

    if (inView) {
      // Do not call el.load() — it aborts in-flight fetches (ERR_ABORTED).
      const tryPlay = () => {
        void el.play().catch(() => {
          /* Autoplay can still be blocked. */
        })
      }
      tryPlay()
      if (el.readyState < HTMLMediaElement.HAVE_CURRENT_DATA) {
        el.addEventListener("canplay", tryPlay, { once: true })
        return () => {
          el.removeEventListener("canplay", tryPlay)
        }
      }
      return
    }

    el.pause()
  }, [failed, inView, shouldLoad, src])

  if (failed) {
    return (
      <div
        ref={rootRef}
        className={cn("relative overflow-hidden bg-[#e1dfdb]", className)}
        aria-hidden
      />
    )
  }

  return (
    <div
      ref={rootRef}
      className={cn("relative overflow-hidden", className)}
      aria-hidden
    >
      {shouldLoad ? (
        <video
          key={src}
          ref={videoRef}
          className={cn(
            "absolute inset-0 size-full object-cover object-top",
            videoClassName,
          )}
          muted
          loop
          playsInline
          preload="none"
          disableRemotePlayback
          onError={() => setFailed(true)}
          {...videoProps}
        >
          <source src={src} type={videoSourceType(src)} />
        </video>
      ) : (
        <div className="absolute inset-0 bg-[#e1dfdb]" aria-hidden />
      )}
    </div>
  )
}
