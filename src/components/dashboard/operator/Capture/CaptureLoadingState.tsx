import { Spinner } from "@/components/ui/spinner"

/** Full-body centered spinner — Guests/Capture first-load chrome. */
export function CaptureLoadingState({ label }: { label: string }) {
  return (
    <div
      className="flex flex-1 items-center justify-center"
      role="status"
      aria-live="polite"
      aria-label={label}
    >
      <Spinner />
    </div>
  )
}
