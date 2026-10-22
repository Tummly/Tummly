import { useEffect } from "react"

import { useFeedbackPageModuleApi } from "@/components/dashboard/operator/Feedback/utils/feedbackPageModuleContext"
import {
  DEMO_TOUR_ACTION_EVENT,
  DEMO_TOUR_AI_PROMPT,
  type DemoTourActionDetail,
} from "@/lib/productDemo/demoTourSession"

type DemoTourActionBridgeProps = {
  openAiAssistant: (input: { prompt: string; send: boolean }) => void
  closeAiAssistant: () => void
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

/**
 * Clicks the target, or its first link / button. Row checkboxes are skipped
 * so a table row opens its record instead of selecting it.
 */
function clickTourTarget(target: string): void {
  const el = document.querySelector<HTMLElement>(`[data-demo-tour="${target}"]`)
  if (!el) return
  const clickable = el.matches("a,button,[role='button']")
    ? el
    : Array.from(
        el.querySelectorAll<HTMLElement>("a,button,[role='button']")
      ).find((node) => node.getAttribute("role") !== "checkbox")
  // Programmatic clicks are not trusted events, so the demo lock lets them through.
  clickable?.click()
}

/**
 * Bridges tour coachmark CTAs into Operator page modules / DOM clicks.
 * Each action runs once, when the visitor presses the step CTA.
 */
export function DemoTourActionBridge({
  openAiAssistant,
  closeAiAssistant,
}: DemoTourActionBridgeProps) {
  const feedbackPage = useFeedbackPageModuleApi()

  useEffect(() => {
    const onAction = (event: Event) => {
      const detail = (event as CustomEvent<DemoTourActionDetail>).detail
      if (detail == null) return

      const { action, target } = detail

      switch (action) {
        case "openFeedbackDetails": {
          const id = graceFeedbackIdFromDom()
          if (id != null) {
            void feedbackPage.openFeedbackDetails(id)
          } else if (target) {
            clickTourTarget(target)
          }
          return
        }
        case "openRecovery": {
          const id = graceFeedbackIdFromDom()
          if (id != null) {
            void feedbackPage.openFeedbackDetailsFromQuery({
              feedbackId: id,
              startRecovery: true,
            })
          }
          return
        }
        case "openGuestProfile":
          clickTourTarget("guest-high-engagement")
          return
        case "openOffer":
          clickTourTarget("offer-primary-row")
          return
        case "openCampaign":
          clickTourTarget("campaign-tour-row")
          return
        case "openAi":
          openAiAssistant({ prompt: DEMO_TOUR_AI_PROMPT, send: false })
          return
        case "showAiAnswer":
          openAiAssistant({ prompt: DEMO_TOUR_AI_PROMPT, send: true })
          return
        case "resetTourUi":
          closeAiAssistant()
          return
        default:
          return
      }
    }

    window.addEventListener(DEMO_TOUR_ACTION_EVENT, onAction)
    return () => window.removeEventListener(DEMO_TOUR_ACTION_EVENT, onAction)
  }, [feedbackPage, openAiAssistant, closeAiAssistant])

  return null
}
