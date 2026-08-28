import {
  operatorDashboardBillingCreditsManagePlanPath,
  operatorDashboardBillingCreditsPath,
} from "@/lib/operatorBillingCredits/billingCreditsPresentation"
import type { CampaignsMessagingChromeAction } from "@/lib/operatorCampaigns/campaignsMessagingCreditChrome"
import type { OperatorDashboardMode } from "@/lib/operatorHome/operatorDashboardPaths"

/** Map Campaigns Messaging chrome CTA kinds to Billing & credits destinations. */
export function pathForCampaignsMessagingChromeAction(
  action: CampaignsMessagingChromeAction,
  mode: OperatorDashboardMode,
  locationId: number
): string {
  switch (action.kind) {
    case "view-usage":
      return operatorDashboardBillingCreditsPath(mode, locationId, {
        tab: "credits-usage",
      })
    case "buy-sms-credits":
      return operatorDashboardBillingCreditsManagePlanPath(mode, locationId, {
        section: "credit-top-ups",
        channel: "sms",
      })
    case "buy-email-credits":
      return operatorDashboardBillingCreditsManagePlanPath(mode, locationId, {
        section: "credit-top-ups",
        channel: "email",
      })
    case "change-plan":
    case "choose-a-plan":
      return operatorDashboardBillingCreditsManagePlanPath(mode, locationId)
    case "update-payment-method":
      return operatorDashboardBillingCreditsPath(mode, locationId, {
        tab: "payment-invoices",
      })
  }
}
