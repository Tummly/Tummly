type ReportsEmptyStateProps = {
  className?: string
}

export function ReportsEmptyState({ className }: ReportsEmptyStateProps) {
  return (
    <div className="flex w-full min-h-[380px] flex-col items-center justify-center gap-7 py-20">
      <div className="flex flex-col items-center justify-start gap-2.5">
        <div className="text-center text-base font-medium text-op-text-primary">
          No report data yet.
        </div>
        <div className="w-80 text-center text-sm font-medium leading-5 text-op-text-muted">
          Create your first QR code and start collecting private guest feedback.
        </div>
      </div>
    </div>
  )
}
