function LoadingBlock({ className }: { className?: string }) {
  return (
    <div
      aria-hidden
      className={`animate-pulse rounded-[18px] bg-white/10 ${className ?? ""}`}
    />
  )
}

export function GuestFeedbackLoading() {
  return (
    <div
      aria-busy="true"
      aria-label="Loading feedback form"
      className="flex w-full flex-col gap-[29px]"
    >
      <div className="flex flex-col gap-6 rounded-[28px] p-5">
        <div className="flex items-center gap-3">
          <LoadingBlock className="size-[42px] rounded" />
          <div className="flex flex-col gap-2">
            <LoadingBlock className="h-6 w-40" />
            <LoadingBlock className="h-3 w-28" />
          </div>
        </div>
        <div className="flex flex-col gap-3">
          <LoadingBlock className="h-8 w-56" />
          <LoadingBlock className="h-12 w-full" />
        </div>
      </div>

      <div className="flex flex-col gap-3.5">
        <LoadingBlock className="min-h-[207px] rounded-[28px]" />
        <LoadingBlock className="min-h-[200px] rounded-[28px]" />
      </div>

      <LoadingBlock className="mx-auto h-5 w-28" />
      <LoadingBlock className="h-[50px] w-full rounded-[54px]" />
    </div>
  )
}
