import { GUEST_FEEDBACK_FORM_STACK_CLASS } from "@/lib/guestFeedback/guestFeedbackLayoutPresentation"

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
      className={GUEST_FEEDBACK_FORM_STACK_CLASS}
    >
      <div className="flex flex-col gap-6 rounded-[28px] p-5 lg:gap-7 lg:p-7">
        <div className="flex items-center gap-3 lg:gap-3.5">
          <LoadingBlock className="size-[42px] rounded lg:size-12" />
          <div className="flex flex-col gap-2">
            <LoadingBlock className="h-6 w-40 lg:h-7 lg:w-48" />
            <LoadingBlock className="h-3 w-28 lg:h-3.5 lg:w-36" />
          </div>
        </div>
        <div className="flex flex-col gap-3">
          <LoadingBlock className="h-8 w-56 lg:h-9 lg:w-72" />
          <LoadingBlock className="h-12 w-full lg:h-14" />
        </div>
      </div>

      <div className="flex flex-col gap-3.5 lg:gap-4">
        <LoadingBlock className="min-h-[clamp(207px,32vh,280px)] rounded-[28px] lg:min-h-[clamp(260px,36vh,380px)]" />
        <LoadingBlock className="min-h-[200px] rounded-[28px] lg:min-h-[240px]" />
      </div>

      <LoadingBlock className="mx-auto h-5 w-28" />
      <LoadingBlock className="h-[50px] w-full rounded-[54px] lg:h-14" />
    </div>
  )
}
