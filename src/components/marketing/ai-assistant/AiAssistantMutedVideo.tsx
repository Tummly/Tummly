import {
  useEffect,
  useRef,
  useState,
  type VideoHTMLAttributes,
} from "react"
import { Pause, Play, Volume2, VolumeX } from "lucide-react"

import { cn } from "@/lib/utils"

type AiAssistantMutedVideoProps = {
  src: string
  /** When true, show mute/unmute only (legacy Guest Loop). Prefer `withPlayerControls`. */
  withAudioToggle?: boolean
  /**
   * Guest Loop player chrome: play/pause, scrubber + duration, and mute.
   * Implies interactive controls (not autoplay-only).
   */
  withPlayerControls?: boolean
  /** Eagerly mount/load media (hero). Playback still waits for viewport. Default false. */
  eager?: boolean
  className?: string
  videoClassName?: string
} & Omit<
  VideoHTMLAttributes<HTMLVideoElement>,
  "src" | "muted" | "controls" | "className" | "autoPlay"
>

function formatVideoTime(seconds: number): string {
  if (!Number.isFinite(seconds) || seconds < 0) {
    return "0:00"
  }
  const total = Math.floor(seconds)
  const mins = Math.floor(total / 60)
  const secs = total % 60
  return `${mins}:${secs.toString().padStart(2, "0")}`
}

const controlButtonClass =
  "inline-flex size-10 shrink-0 cursor-pointer items-center justify-center rounded-full bg-transparent text-white transition-colors hover:bg-white/15 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-white/50"

/**
 * Looping marketing video. Starts muted. Plays only while in the viewport so
 * off-screen clips stay paused (AI Assistant + shared marketing CTAs).
 * Guest Loop can opt into full player chrome via `withPlayerControls`.
 */
export function AiAssistantMutedVideo({
  src,
  withAudioToggle = false,
  withPlayerControls = false,
  eager = false,
  className,
  videoClassName,
  ...videoProps
}: AiAssistantMutedVideoProps) {
  const showPlayer = withPlayerControls || withAudioToggle
  const rootRef = useRef<HTMLDivElement>(null)
  const videoRef = useRef<HTMLVideoElement>(null)
  const [muted, setMuted] = useState(true)
  const [failed, setFailed] = useState(false)
  const [shouldLoad, setShouldLoad] = useState(eager)
  const [inView, setInView] = useState(false)
  const [playing, setPlaying] = useState(false)
  const [userPaused, setUserPaused] = useState(false)
  const [currentTime, setCurrentTime] = useState(0)
  const [duration, setDuration] = useState(0)

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
        // Play only while the user can see the clip — no early rootMargin.
        rootMargin: "0px",
        threshold: 0.25,
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

    if (inView && !userPaused) {
      if (el.readyState < HTMLMediaElement.HAVE_CURRENT_DATA) {
        el.load()
      }
      void el.play().catch(() => {
        /* Autoplay can still be blocked; unmute after a gesture may retry. */
      })
    } else {
      el.pause()
    }
  }, [inView, muted, shouldLoad, src, userPaused])

  useEffect(() => {
    const el = videoRef.current
    if (el == null || !shouldLoad || !showPlayer) {
      return
    }

    const onTimeUpdate = () => setCurrentTime(el.currentTime)
    const onDurationChange = () => {
      if (Number.isFinite(el.duration)) {
        setDuration(el.duration)
      }
    }
    const onPlay = () => setPlaying(true)
    const onPause = () => setPlaying(false)

    el.addEventListener("timeupdate", onTimeUpdate)
    el.addEventListener("durationchange", onDurationChange)
    el.addEventListener("loadedmetadata", onDurationChange)
    el.addEventListener("play", onPlay)
    el.addEventListener("pause", onPause)
    onDurationChange()
    setPlaying(!el.paused)

    return () => {
      el.removeEventListener("timeupdate", onTimeUpdate)
      el.removeEventListener("durationchange", onDurationChange)
      el.removeEventListener("loadedmetadata", onDurationChange)
      el.removeEventListener("play", onPlay)
      el.removeEventListener("pause", onPause)
    }
  }, [shouldLoad, showPlayer, src])

  const togglePlay = () => {
    const el = videoRef.current
    if (el == null) {
      return
    }
    if (el.paused) {
      setUserPaused(false)
      void el.play().catch(() => {})
    } else {
      setUserPaused(true)
      el.pause()
    }
  }

  const toggleMute = () => {
    setMuted((prev) => !prev)
    const el = videoRef.current
    if (el != null && el.paused && !userPaused) {
      void el.play().catch(() => {})
    }
  }

  const onSeek = (next: number) => {
    const el = videoRef.current
    if (el == null || !Number.isFinite(next)) {
      return
    }
    el.currentTime = next
    setCurrentTime(next)
  }

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
          preload={eager || showPlayer ? "metadata" : "none"}
          onError={() => setFailed(true)}
          onClick={showPlayer ? togglePlay : undefined}
          {...videoProps}
        >
          <source src={src} type="video/mp4" />
        </video>
      ) : (
        <div className="absolute inset-0 bg-[#e8e8e8]" aria-hidden />
      )}

      {showPlayer && shouldLoad ? (
        <div
          className={cn(
            "absolute inset-x-0 bottom-0 z-10 flex items-center gap-2 px-3 py-3",
            "bg-gradient-to-t from-[#141414]/80 via-[#141414]/45 to-transparent",
            "sm:gap-3 sm:px-4 sm:py-4",
          )}
        >
          <button
            type="button"
            className={controlButtonClass}
            aria-label={playing ? "Pause video" : "Play video"}
            onClick={(event) => {
              event.stopPropagation()
              togglePlay()
            }}
          >
            {playing ? (
              <Pause className="size-5 fill-current" aria-hidden />
            ) : (
              <Play className="size-5 fill-current" aria-hidden />
            )}
          </button>

          <span className="hidden shrink-0 font-sans text-xs font-medium tabular-nums text-white sm:inline">
            {formatVideoTime(currentTime)}
          </span>

          <label className="flex min-w-0 flex-1 items-center">
            <span className="sr-only">Video progress</span>
            <input
              type="range"
              min={0}
              max={duration > 0 ? duration : 0}
              step={0.1}
              value={duration > 0 ? Math.min(currentTime, duration) : 0}
              disabled={duration <= 0}
              aria-valuetext={`${formatVideoTime(currentTime)} of ${formatVideoTime(duration)}`}
              className={cn(
                "h-1.5 w-full cursor-pointer appearance-none rounded-full bg-white/30",
                "accent-white disabled:cursor-not-allowed",
                "[&::-webkit-slider-thumb]:size-3.5 [&::-webkit-slider-thumb]:appearance-none",
                "[&::-webkit-slider-thumb]:rounded-full [&::-webkit-slider-thumb]:bg-white",
                "[&::-moz-range-thumb]:size-3.5 [&::-moz-range-thumb]:rounded-full",
                "[&::-moz-range-thumb]:border-0 [&::-moz-range-thumb]:bg-white",
              )}
              onClick={(event) => event.stopPropagation()}
              onChange={(event) => {
                onSeek(Number(event.target.value))
              }}
            />
          </label>

          <span className="shrink-0 font-sans text-xs font-medium tabular-nums text-white">
            <span className="sm:hidden">{formatVideoTime(currentTime)} / </span>
            {formatVideoTime(duration)}
          </span>

          <button
            type="button"
            className={controlButtonClass}
            aria-label={muted ? "Unmute video" : "Mute video"}
            aria-pressed={!muted}
            onClick={(event) => {
              event.stopPropagation()
              toggleMute()
            }}
          >
            {muted ? (
              <VolumeX className="size-5" aria-hidden />
            ) : (
              <Volume2 className="size-5" aria-hidden />
            )}
          </button>
        </div>
      ) : null}
    </div>
  )
}
