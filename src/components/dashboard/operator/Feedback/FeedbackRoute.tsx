import { useEffect, useRef } from "react"
import { useLocation, useNavigate, useOutletContext } from "react-router-dom"

import { FeedbackPage } from "@/components/dashboard/operator/Feedback/FeedbackPage"
import { useFeedbackPageModuleApi } from "@/components/dashboard/operator/Feedback/utils/feedbackPageModuleContext"
import type { DashboardOutletContext } from "@/components/dashboard/operator/Dashboard"
import { parseRecoveryDraftActionRouterState } from "@/lib/operatorFeedback/recoveryDraftAction"
import { toast } from "sonner"

export function FeedbackRoute() {
  const { selectedLocationId, locations } =
    useOutletContext<DashboardOutletContext>()
  const feedbackPageModule = useFeedbackPageModuleApi()
  const syncFeedbackRef = useRef(feedbackPageModule.syncWorkspace)
  const location = useLocation()
  const navigate = useNavigate()
  const consumedRecoveryDraftKeyRef = useRef<string | null>(null)

  syncFeedbackRef.current = feedbackPageModule.syncWorkspace

  useEffect(() => {
    if (selectedLocationId == null) {
      return
    }

    void syncFeedbackRef.current({
      selectedLocationId,
      locations: locations.map((locationRow) => ({
        id: locationRow.id,
        locationName: locationRow.locationName,
      })),
    })
  }, [selectedLocationId, locations])

  useEffect(() => {
    const payload = parseRecoveryDraftActionRouterState(location.state)
    if (payload == null) {
      return
    }
    const key = `${payload.feedbackId}:${payload.intent}:${location.key}`
    if (consumedRecoveryDraftKeyRef.current === key) {
      return
    }
    consumedRecoveryDraftKeyRef.current = key
    navigate(location.pathname + location.search, { replace: true, state: null })
    void feedbackPageModule.openFromDraftAction(payload).catch(() => {
      toast.error("Could not open recovery. Please try again.")
    })
  }, [
    feedbackPageModule,
    location.key,
    location.pathname,
    location.search,
    location.state,
    navigate,
  ])

  return <FeedbackPage />
}
