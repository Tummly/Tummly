import { cn } from "@/lib/utils"

type GuestFeedbackGlowTone = "accent" | "neutral"

type GuestFeedbackGlowProps = {
  tone?: GuestFeedbackGlowTone
  className?: string
}

/**
 * Figma Guest-Loop-MVP Rectangle 6 — thank-you / unlock bottom bloom.
 * Frame: 393×286 at bottom (node 6829:13688). Exact radial + 0.2 fill opacity.
 */
const ACCENT_GLOW_SVG =
  "url(\"data:image/svg+xml;utf8,<svg viewBox='0 0 393 286' xmlns='http://www.w3.org/2000/svg' preserveAspectRatio='none'><rect x='0' y='0' height='100%' width='100%' fill='url(%23grad)' opacity='0.2'/><defs><radialGradient id='grad' gradientUnits='userSpaceOnUse' cx='0' cy='0' r='10' gradientTransform='matrix(0.0000019271 37.339 -51.308 0.0000049586 196 384.64)'><stop stop-color='rgba(20,162,71,0.6)' offset='0.33181'/><stop stop-color='rgba(19,125,57,0.7)' offset='0.49886'/><stop stop-color='rgba(18,89,43,0.8)' offset='0.66591'/><stop stop-color='rgba(16,52,29,0.9)' offset='0.83295'/><stop stop-color='rgba(16,33,22,0.95)' offset='0.91648'/><stop stop-color='rgba(15,15,15,1)' offset='1'/></radialGradient></defs></svg>\")"

/** Neutral (need-consent) twin — same geometry, gray stops. */
const NEUTRAL_GLOW_SVG =
  "url(\"data:image/svg+xml;utf8,<svg viewBox='0 0 393 286' xmlns='http://www.w3.org/2000/svg' preserveAspectRatio='none'><rect x='0' y='0' height='100%' width='100%' fill='url(%23grad)' opacity='0.2'/><defs><radialGradient id='grad' gradientUnits='userSpaceOnUse' cx='0' cy='0' r='10' gradientTransform='matrix(0.0000019271 37.339 -51.308 0.0000049586 196 384.64)'><stop stop-color='rgba(154,154,154,0.6)' offset='0.33181'/><stop stop-color='rgba(119,119,119,0.7)' offset='0.49886'/><stop stop-color='rgba(85,85,85,0.8)' offset='0.66591'/><stop stop-color='rgba(50,50,50,0.9)' offset='0.83295'/><stop stop-color='rgba(32,32,32,0.95)' offset='0.91648'/><stop stop-color='rgba(15,15,15,1)' offset='1'/></radialGradient></defs></svg>\")"

/**
 * Bottom radial glow — Figma Rectangle 6 (6829:13688).
 * Must sit on the shell root (full viewport width), not the narrow content column.
 */
export function GuestFeedbackGlow({
  tone = "accent",
  className,
}: GuestFeedbackGlowProps) {
  return (
    <div
      aria-hidden
      data-guest-feedback-glow={tone}
      className={cn(
        "pointer-events-none absolute inset-x-0 bottom-0 z-0 h-[286px] w-full",
        className
      )}
      style={{
        backgroundImage: tone === "accent" ? ACCENT_GLOW_SVG : NEUTRAL_GLOW_SVG,
      }}
    />
  )
}
