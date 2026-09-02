import aiIconPng from "@/assets/svg/ui-icons/ai-icon.png"
import { ArrowRight, CheckCircle2, TrendingUp, Users, MessageSquare, Gift } from "lucide-react"
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
} from "@/components/ui/dialog"
import { Button } from "@/components/ui/button"

type ReportsWeeklyBriefDialogProps = {
  open: boolean
  onOpenChange: (open: boolean) => void
  locationName?: string
  dateRangeLabel?: string
  onNavigateToFeedback?: () => void
  onNavigateToCampaigns?: () => void
  onNavigateToCapture?: () => void
}

export function ReportsWeeklyBriefDialog({
  open,
  onOpenChange,
  locationName = "Mehmet's Grill",
  dateRangeLabel = "Last 7 days",
  onNavigateToFeedback,
  onNavigateToCampaigns,
  onNavigateToCapture,
}: ReportsWeeklyBriefDialogProps) {
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent
        className="z-[200] max-h-[90vh] w-full max-w-2xl overflow-y-auto rounded-md border border-op-border-default bg-op-card-background p-6 text-op-text-primary shadow-2xl"
        overlayClassName="z-[190] bg-black/60 backdrop-blur-xs"
      >
        <DialogHeader className="text-left">
          <div className="flex items-center gap-2 text-xs font-semibold text-op-action-primary uppercase tracking-wider">
            <img src={aiIconPng} alt="" className="size-4 shrink-0 brightness-0 invert" />
            <span>AI Weekly Brief — {dateRangeLabel}</span>
          </div>
          <DialogTitle className="text-xl font-bold text-op-text-primary">
            Weekly Guest Loop Brief for {locationName}
          </DialogTitle>
          <DialogDescription className="text-xs text-op-text-muted">
            Executive summary of guest engagement, feedback sentiment, and campaign effectiveness.
          </DialogDescription>
        </DialogHeader>

        <div className="flex flex-col gap-6 pt-2">
          {/* Executive Summary */}
          <div className="rounded-sm border border-op-border-default bg-op-surface-secondary/60 p-4">
            <p className="text-sm font-medium leading-relaxed text-op-text-primary">
              Overall loop health is strong this period. You received <strong className="text-op-action-primary">42 feedback messages</strong> and captured <strong className="text-op-action-primary">28 contactable guests</strong>. Delivery inserts generated the highest engagement volume (72 scans, 18 feedback responses).
            </p>
          </div>

          {/* Key Pillars */}
          <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
            <div className="flex flex-col gap-1 rounded-sm border border-op-border-default bg-op-background-primary p-3.5">
              <div className="flex items-center gap-2 text-xs font-medium text-op-text-muted">
                <MessageSquare className="size-3.5 text-op-action-primary" />
                <span>Feedback Health</span>
              </div>
              <span className="text-lg font-bold text-op-text-primary">88% Positive</span>
              <span className="text-xs text-op-text-muted">42 reviews collected</span>
            </div>

            <div className="flex flex-col gap-1 rounded-sm border border-op-border-default bg-op-background-primary p-3.5">
              <div className="flex items-center gap-2 text-xs font-medium text-op-text-muted">
                <Users className="size-3.5 text-op-action-primary" />
                <span>Guest Capture</span>
              </div>
              <span className="text-lg font-bold text-op-text-primary">66.7% Rate</span>
              <span className="text-xs text-op-text-muted">28 contacts opt-ins</span>
            </div>

            <div className="flex flex-col gap-1 rounded-sm border border-op-border-default bg-op-background-primary p-3.5">
              <div className="flex items-center gap-2 text-xs font-medium text-op-text-muted">
                <Gift className="size-3.5 text-op-action-primary" />
                <span>Offer Impact</span>
              </div>
              <span className="text-lg font-bold text-op-text-primary">12 Redeemed</span>
              <span className="text-xs text-op-text-muted">31.5% claim-to-use</span>
            </div>
          </div>

          {/* Action Items List */}
          <div className="flex flex-col gap-3">
            <h3 className="text-sm font-semibold text-op-text-primary">
              High Priority Recommended Steps
            </h3>

            <div className="flex flex-col gap-2.5">
              <div className="flex items-start justify-between gap-3 rounded-sm border border-op-border-default bg-op-background-primary p-3">
                <div className="flex items-start gap-2.5">
                  <CheckCircle2 className="mt-0.5 size-4 shrink-0 text-op-action-primary" />
                  <div className="flex flex-col gap-0.5">
                    <span className="text-xs font-semibold text-op-text-primary">
                      Follow up with 6 guests awaiting reply
                    </span>
                    <span className="text-xs text-op-text-muted">
                      Shared contact details with specific service inquiries.
                    </span>
                  </div>
                </div>
                {onNavigateToFeedback && (
                  <Button
                    type="button"
                    variant="ghost"
                    size="sm"
                    className="h-7 px-2 text-xs text-op-action-primary hover:bg-op-surface-secondary"
                    onClick={() => {
                      onOpenChange(false)
                      onNavigateToFeedback()
                    }}
                  >
                    Open <ArrowRight className="ml-1 size-3" />
                  </Button>
                )}
              </div>

              <div className="flex items-start justify-between gap-3 rounded-sm border border-op-border-default bg-op-background-primary p-3">
                <div className="flex items-start gap-2.5">
                  <TrendingUp className="mt-0.5 size-4 shrink-0 text-op-action-primary" />
                  <div className="flex flex-col gap-0.5">
                    <span className="text-xs font-semibold text-op-text-primary">
                      Launch quiet-day follow-up campaign
                    </span>
                    <span className="text-xs text-op-text-muted">
                      Your quiet-day offer drove 12 redemptions this week.
                    </span>
                  </div>
                </div>
                {onNavigateToCampaigns && (
                  <Button
                    type="button"
                    variant="ghost"
                    size="sm"
                    className="h-7 px-2 text-xs text-op-action-primary hover:bg-op-surface-secondary"
                    onClick={() => {
                      onOpenChange(false)
                      onNavigateToCampaigns()
                    }}
                  >
                    Launch <ArrowRight className="ml-1 size-3" />
                  </Button>
                )}
              </div>
            </div>
          </div>

          <div className="flex items-center justify-end gap-3 pt-2">
            <Button
              type="button"
              variant="outline"
              onClick={() => onOpenChange(false)}
              className="h-9 text-xs"
            >
              Close
            </Button>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  )
}
