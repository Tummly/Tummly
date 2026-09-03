import type { BillingCreditsAccessLevel } from "@/lib/operatorBillingCredits/billingCreditsPresentation"
import {
  operatorDashboardBillingCreditsManagePlanPath,
  operatorDashboardBillingCreditsPath,
} from "@/lib/operatorBillingCredits/billingCreditsPresentation"
import type { OperatorDashboardMode } from "@/lib/operatorHome/operatorDashboardPaths"
import {
  isAccountLockedBillingStatus,
  resolveLockRestorationCause,
} from "@/lib/operatorHome/lockAlertPresentation"

export type ShopPaidWriteHelperCta = {
  label: string
  href: string
}

export type ShopPaidWriteChrome = {
  purchaseDisabled: boolean
  helperCta: ShopPaidWriteHelperCta | null
}

export type ShopPaidWriteChromeContext = {
  billingStatus: string
  subscriptionPlan: string
  chargebackRestricted?: boolean
  accessLevel: BillingCreditsAccessLevel
  permissionRole: string
  mode: OperatorDashboardMode
  locationId: number
}

export function resolveShopPaidWriteChrome(
  input: ShopPaidWriteChromeContext
): ShopPaidWriteChrome {
  if (input.chargebackRestricted) {
    return { purchaseDisabled: true, helperCta: null }
  }

  if (!isAccountLockedBillingStatus(input.billingStatus)) {
    return { purchaseDisabled: false, helperCta: null }
  }

  const cause = resolveLockRestorationCause({
    billingStatus: input.billingStatus,
    subscriptionPlan: input.subscriptionPlan,
    isPilot: input.subscriptionPlan === "Pilot",
  });

  if (cause == null || input.accessLevel !== "manage") {
    return { purchaseDisabled: true, helperCta: null }
  }

  if (cause === "pilot") {
    if (input.permissionRole !== "Owner") {
      return { purchaseDisabled: true, helperCta: null }
    }
    return {
      purchaseDisabled: true,
      helperCta: {
        label: "Choose a plan",
        href: operatorDashboardBillingCreditsManagePlanPath(
          input.mode,
          input.locationId
        ),
      },
    }
  }

  const canUpdatePayment =
    input.permissionRole === "Owner"
    || input.permissionRole === "Billing Admin"
    || input.permissionRole === "Admin";

  if (!canUpdatePayment) {
    return { purchaseDisabled: true, helperCta: null }
  }

  return {
    purchaseDisabled: true,
    helperCta: {
      label: "Update payment method",
      href: operatorDashboardBillingCreditsPath(input.mode, input.locationId, {
        tab: "payment-invoices",
      }),
    },
  }
}
