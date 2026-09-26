import { XIcon } from "lucide-react"

import activateTummlyPilotHero from "@/assets/operator-home/activate-tummly-pilot-hero.webp"
import { Button } from "@/components/ui/button"
import {
  Dialog,
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog"
import {
  ACTIVATE_TUMMLY_PILOT_DIALOG_ACTIONS_CLASS,
  ACTIVATE_TUMMLY_PILOT_DIALOG_BODY_CLASS,
  ACTIVATE_TUMMLY_PILOT_DIALOG_CONTENT_CLASS,
  ACTIVATE_TUMMLY_PILOT_DIALOG_COPY,
  ACTIVATE_TUMMLY_PILOT_DIALOG_HEADER_CLASS,
  ACTIVATE_TUMMLY_PILOT_DIALOG_HERO_CLASS,
  ACTIVATE_TUMMLY_PILOT_DIALOG_TITLE_CLASS,
  ACTIVATE_TUMMLY_PILOT_DIALOG_TITLE_ROW_CLASS,
  activateDialogCopyForPlan,
} from "@/lib/operatorHome/activateTummlyPilotDialogPresentation"

export type ActivateTummlyPilotDialogProps = {
  open: boolean
  onOpenChange: (open: boolean) => void
  onStartPilot: () => void
  onViewPlans: () => void
  /** Pilot (default) or Free — selects dialog copy. */
  subscriptionPlan?: string
}

export function ActivateTummlyPilotDialog({
  open,
  onOpenChange,
  onStartPilot,
  onViewPlans,
  subscriptionPlan = "Pilot",
}: ActivateTummlyPilotDialogProps) {
  const copy =
    subscriptionPlan === "Free"
      ? activateDialogCopyForPlan("Free")
      : ACTIVATE_TUMMLY_PILOT_DIALOG_COPY

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent
        showCloseButton={false}
        className={ACTIVATE_TUMMLY_PILOT_DIALOG_CONTENT_CLASS}
      >
        <div className={ACTIVATE_TUMMLY_PILOT_DIALOG_HEADER_CLASS}>
          <div className={ACTIVATE_TUMMLY_PILOT_DIALOG_TITLE_ROW_CLASS}>
            <DialogHeader className="min-w-0 flex-1 gap-3 text-left">
              <DialogTitle className={ACTIVATE_TUMMLY_PILOT_DIALOG_TITLE_CLASS}>
                {copy.title}
              </DialogTitle>
              <DialogDescription
                className={ACTIVATE_TUMMLY_PILOT_DIALOG_BODY_CLASS}
              >
                {copy.body}
              </DialogDescription>
            </DialogHeader>
            <DialogClose asChild>
              <Button
                type="button"
                variant="op-collapse"
                size="icon"
                aria-label="Close"
                className="shrink-0"
              >
                <XIcon aria-hidden />
              </Button>
            </DialogClose>
          </div>

          <div className={ACTIVATE_TUMMLY_PILOT_DIALOG_ACTIONS_CLASS}>
            <Button type="button" variant="op-primary" onClick={onStartPilot}>
              {copy.startCta}
            </Button>
            <Button type="button" variant="op-tertiary" onClick={onViewPlans}>
              {copy.viewPlansCta}
            </Button>
          </div>
        </div>

        <div className={ACTIVATE_TUMMLY_PILOT_DIALOG_HERO_CLASS}>
          <img
            src={activateTummlyPilotHero}
            alt=""
            width={560}
            height={384}
            className="absolute inset-0 size-full object-cover"
            decoding="async"
          />
        </div>
      </DialogContent>
    </Dialog>
  )
}
