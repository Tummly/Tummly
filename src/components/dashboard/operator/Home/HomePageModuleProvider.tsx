import { createElement, useEffect, useState, type ReactNode } from "react"
import { toast } from "sonner"

import {
  correctFeedbackClassification,
  getChecklistAcks,
  getFeedback,
  getFeedbackDetails,
  getHomePerformance,
  setChecklistAcks,
} from "@/api/dashboardApi"
import { homePageModuleContext } from "@/components/dashboard/operator/Home/utils/homePageModuleContext"
import { useDashboardUiStoreApi } from "@/components/dashboard/operator/DashboardUiStoreProvider"
import { connectFeedbackHomeHub } from "@/lib/operatorHome/connectFeedbackHomeHub"
import {
  createOperatorHomePageModule,
} from "@/lib/operatorHome/createOperatorHomePageModule"

function openSmartGuestLink(url: string): void {
  window.open(url, "_blank", "noopener,noreferrer")
}

async function copyText(
  text: string
): Promise<{ ok: true } | { ok: false; error: string }> {
  try {
    await navigator.clipboard.writeText(text)
    return { ok: true }
  } catch {
    return {
      ok: false,
      error: "Could not copy Smart Guest Link. Please try again.",
    }
  }
}

export function HomePageModuleProvider({
  children,
}: {
  children: ReactNode
}) {
  const dashboardUiStore = useDashboardUiStoreApi()
  const [pageModule] = useState(() =>
    createOperatorHomePageModule({
      getFeedback,
      getHomePerformance,
      getHomePerformanceDateRange: () =>
        dashboardUiStore.getState().homePerformanceDateRange,
      getFeedbackDetails,
      correctClassification: async (feedbackId, sentiment) => {
        const result = await correctFeedbackClassification(
          feedbackId,
          sentiment
        )
        return {
          classificationStatus: result.classificationStatus,
          sentiment: result.sentiment,
          detectedTags: result.detectedTags,
        }
      },
      getChecklistAcks,
      setChecklistAcks,
      copyText,
      openSmartGuestLink,
      connectRealtime: connectFeedbackHomeHub,
      onPerformanceLoadError: (message) => {
        toast.error(message)
      },
    })
  )

  useEffect(() => {
    void pageModule.connect()
    return () => {
      void pageModule.disconnect()
    }
  }, [pageModule])

  return createElement(
    homePageModuleContext.Provider,
    { value: pageModule },
    children
  )
}
