import { useEffect } from "react"

import { useFeedbackPageModuleApi } from "@/components/dashboard/operator/Feedback/utils/feedbackPageModuleContext"
import {
  DEMO_TOUR_ACTION_EVENT,
  type DemoTourActionDetail,
} from "@/lib/productDemo/demoTourSession"

type DemoTourActionBridgeProps = {
  openAiAssistant: (prefill?: string) => void
}

function graceFeedbackIdFromDom(): number | null {
  const row = document.querySelector<HTMLElement>(
    '[data-demo-tour="feedback-needs-attention-row"]'
  )
  const raw = row?.getAttribute("data-feedback-id")
  if (!raw) return null
  const id = Number.parseInt(raw, 10)
  return Number.isFinite(id) ? id : null
}

function clickTourTarget(target: string): void {
  const el = document.querySelector<HTMLElement>(`[data-demo-tour="${target}"]`)
  if (!el) return
  const clickable =
    el.matches("a,button,[role='button']")
      ? el
      : el.querySelector<HTMLElement>("a,button,[role='button']")
  clickable?.click()
}

/**
 * Bridges tour coachmark actions into Operator page modules / DOM clicks.
 */
export function DemoTourActionBridge({
  openAiAssistant,
}: DemoTourActionBridgeProps) {
  const feedbackPage = useFeedbackPageModuleApi()

  useEffect(() => {
    const onAction = (event: Event) => {
      const detail = (event as CustomEvent<DemoTourActionDetail>).detail
      if (detail == null) return

      const { action, target } = detail

      if (action === "navigate" && target) {
        clickTourTarget(target)
        return
      }

      if (action === "openFeedbackDetails") {
        const id = graceFeedbackIdFromDom()
        if (id != null) {
          void feedbackPage.openFeedbackDetails(id)
        } else if (target) {
          clickTourTarget(target)
        }
        return
      }

      if (action === "openRecovery") {
        const id = graceFeedbackIdFromDom()
        if (id != null) {
          void feedbackPage.openFeedbackDetailsFromQuery({
            feedbackId: id,
            startRecovery: true,
          })
        }
        return
      }

      if (action === "openAi" || action === "showAiAnswer") {
        openAiAssistant(
          "Summarise feedback from the last 30 days and tell me what needs attention."
        )
        return
      }

      if (action === "openWeeklyBrief" && target) {
        clickTourTarget(target)
        return
      }

      if (action === "openGuestProfile") {
        clickTourTarget("guest-high-engagement")
        return
      }

      if (
        (action === "openOffer" || action === "openCampaign") &&
        target
      ) {
        clickTourTarget(target)
      }
    }

    window.addEventListener(DEMO_TOUR_ACTION_EVENT, onAction)
    return () => window.removeEventListener(DEMO_TOUR_ACTION_EVENT, onAction)
  }, [feedbackPage, openAiAssistant])

  return null
}
