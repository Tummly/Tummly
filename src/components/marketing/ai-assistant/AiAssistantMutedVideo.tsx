import { useEffect, useRef, useState, type VideoHTMLAttributes } from "react"
import { Volume2, VolumeX } from "lucide-react"

import { cn } from "@/lib/utils"

type AiAssistantMutedVideoProps = {
  src: string
  /** When true, show a mute/unmute control (Guest Loop audio video). */
  withAudioToggle?: boolean
  /** Eagerly start when near viewport (hero). Default false. */
  eager?: boolean
  className?: string
  videoClassName?: string
} & Omit<
  VideoHTMLAttributes<HTMLVideoElement>,
  "src" | "muted" | "controls" | "className" | "autoPlay"
>

/**
 * Looping marketing video. Starts muted. Plays only while in (or near) the
 * viewport so multiple page videos do not decode at once.
 */
export function AiAssistantMutedVideo({
  src,
  withAudioToggle = false,
  eager = false,
  className,
  videoClassName,
  ...videoProps
}: AiAssistantMutedVideoProps) {
  const rootRef = useRef<HTMLDivElement>(null)
  const videoRef = useRef<HTMLVideoElement>(null)
  const [muted, setMuted] = useState(true)
  const [failed, setFailed] = useState(false)
  const [shouldLoad, setShouldLoad] = useState(eager)
  const [inView, setInView] = useState(false)

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
      {
        root: null,
        // Start loading slightly before the video enters the viewport.
        rootMargin: "120px 0px",
        threshold: 0.15,
      },
    )

    observer.observe(root)
    return () => observer.disconnect()
  }, [])

  useEffect(() => {
    const el = videoRef.current
    if (el == null || !shouldLoad) {
      return
    }

    el.muted = muted

    if (inView) {
      // preload="none" needs an explicit load before play on first reveal.
      if (el.readyState < HTMLMediaElement.HAVE_CURRENT_DATA) {
        el.load()
      }
      void el.play().catch(() => {
        /* Autoplay can still be blocked; unmute after a gesture may retry. */
      })
    } else {
      el.pause()
    }
  }, [inView, muted, shouldLoad, src])

  if (failed) {
    return (
      <div
        ref={rootRef}
        className={cn("relative overflow-hidden bg-[#e8e8e8]", className)}
        aria-hidden
      />
    )
  }

  return (
    <div ref={rootRef} className={cn("relative overflow-hidden", className)}>
      {shouldLoad ? (
        <video
          key={src}
          ref={videoRef}
          className={cn(
            "absolute inset-0 size-full object-cover",
            videoClassName,
          )}
          muted
          loop
          playsInline
          preload={eager ? "metadata" : "none"}
          onError={() => setFailed(true)}
          {...videoProps}
        >
          <source src={src} type="video/mp4" />
        </video>
      ) : (
        <div className="absolute inset-0 bg-[#e8e8e8]" aria-hidden />
      )}
      {withAudioToggle && shouldLoad ? (
        <button
          type="button"
          className="absolute right-4 bottom-4 z-10 inline-flex size-10 cursor-pointer items-center justify-center rounded-full bg-[#141414]/70 text-white transition-colors hover:bg-[#141414]/90 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-white/50"
          aria-label={muted ? "Unmute video" : "Mute video"}
          aria-pressed={!muted}
          onClick={() => {
            setMuted((prev) => !prev)
            const el = videoRef.current
            if (el != null) {
              void el.play().catch(() => {})
            }
          }}
        >
          {muted ? (
            <VolumeX className="size-5" aria-hidden />
          ) : (
            <Volume2 className="size-5" aria-hidden />
          )}
        </button>
      ) : null}
    </div>
  )
}
