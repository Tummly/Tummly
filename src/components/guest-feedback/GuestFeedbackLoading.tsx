function LoadingBlock({ className }: { className?: string }) {
  return (
    <div
      aria-hidden
      className={`animate-pulse rounded-[4px] bg-white/10 ${className ?? ""}`}
    />
  )
}

export function GuestFeedbackLoading() {
  return (
    <div
      aria-busy="true"
      aria-label="Loading feedback form"
      className="flex w-full flex-col gap-10"
    >
      <div className="flex flex-col gap-2">
        <LoadingBlock className="h-8 w-full max-w-[320px]" />
        <LoadingBlock className="h-4 w-40" />
      </div>

      <div className="flex flex-col gap-3">
        <LoadingBlock className="h-[50px] w-full" />
        <LoadingBlock className="h-[50px] w-full" />
        <LoadingBlock className="h-[183px] w-full" />
      </div>

      <LoadingBlock className="h-[50px] w-full rounded-[54px]" />
    </div>
  )
}
