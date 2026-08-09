import type { CampaignBillingBalancesPayload } from "@/lib/operatorCampaigns/campaignMessagingBalances"

/**
 * Shared Billing balances adapter for overview Messaging usage and wizard
 * Channel meters. Pass the same value from CampaignsPageModuleProvider and
 * CampaignsPage wizard so cutover stays in lockstep.
 *
 * Undefined until the Billing balances API is wired — both surfaces then omit
 * the adapter and keep fixtures (no silent live fallback).
 */
export type LoadCampaignMessagingBalances =
  () => Promise<CampaignBillingBalancesPayload>

export const loadCampaignMessagingBalances:
  | LoadCampaignMessagingBalances
  | undefined = undefined
