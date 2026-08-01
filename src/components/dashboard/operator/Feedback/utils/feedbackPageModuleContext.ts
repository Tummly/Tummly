import { createContext, useContext } from "react"

import type { OperatorFeedbackPageModule } from "@/lib/operatorFeedback/createOperatorFeedbackPageModule"

export const feedbackPageModuleContext =
  createContext<OperatorFeedbackPageModule | null>(null)

export function useFeedbackPageModuleApi(): OperatorFeedbackPageModule {
  const pageModule = useContext(feedbackPageModuleContext)
  if (pageModule == null) {
    throw new Error(
      "useFeedbackPageModule must be used within FeedbackPageModuleProvider"
    )
  }
  return pageModule
}
