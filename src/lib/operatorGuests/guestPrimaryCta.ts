/**
 * Guests table Actions + Guest details Drawer header — Create campaign vs Start recovery.
 * Swap to Start recovery only when not marketing eligible and needs recovery.
 */

export type GuestPrimaryCtaInput = {
  marketingEligible: boolean
  needsRecovery: boolean
  recoveryFeedbackId: number | null
}

export type GuestCreateCampaignLabel =
  | "Create campaign"
  | "Create campaign with guest"

export type GuestPrimaryCta =
  | {
      kind: "start-recovery"
      feedbackId: number
      label: "Start recovery"
    }
  | {
      kind: "create-campaign"
      enabled: boolean
      label: GuestCreateCampaignLabel
    }

export function resolveGuestPrimaryCta(
  input: GuestPrimaryCtaInput,
  options: { createCampaignLabel?: GuestCreateCampaignLabel } = {}
): GuestPrimaryCta {
  const createLabel = options.createCampaignLabel ?? "Create campaign"

  if (
    !input.marketingEligible
    && input.needsRecovery
    && input.recoveryFeedbackId != null
  ) {
    return {
      kind: "start-recovery",
      feedbackId: input.recoveryFeedbackId,
      label: "Start recovery",
    }
  }

  return {
    kind: "create-campaign",
    enabled: input.marketingEligible,
    label: createLabel,
  }
}
