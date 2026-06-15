/** Shown while Zustand auth persist rehydrates or session routing is resolved. */
export function AuthSessionLoading() {
  return (
    <div
      className="flex min-h-[12rem] flex-1 items-center justify-center"
      role="status"
      aria-live="polite"
      aria-label="Loading session"
    >
      <div className="h-8 w-8 animate-spin rounded-full border-2 border-[#d2d2d2] border-t-[#14a74a]" />
    </div>
  )
}
