import { useEffect, useRef } from "react"
import { useOutletContext } from "react-router-dom"

import { FeedbackPage } from "@/components/dashboard/operator/Feedback/FeedbackPage"
import { useFeedbackPageModuleApi } from "@/components/dashboard/operator/Feedback/utils/feedbackPageModuleContext"
import type { DashboardOutletContext } from "@/components/dashboard/operator/Dashboard"

export function FeedbackRoute() {
  const { selectedLocationId, locations } =
    useOutletContext<DashboardOutletContext>()
  const feedbackPageModule = useFeedbackPageModuleApi()
  const syncFeedbackRef = useRef(feedbackPageModule.syncWorkspace)

  syncFeedbackRef.current = feedbackPageModule.syncWorkspace

  useEffect(() => {
    if (selectedLocationId == null) {
      return
    }

    void syncFeedbackRef.current({
      selectedLocationId,
      locations: locations.map((location) => ({
        id: location.id,
        locationName: location.locationName,
      })),
    })
  }, [selectedLocationId, locations])

  return <FeedbackPage />
}
