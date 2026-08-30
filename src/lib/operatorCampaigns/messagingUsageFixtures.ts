/**
 * Campaigns Messaging usage fixtures + overview view model (ticket 23 / lock 09).
 * Shared source for overview + wizard until live Billing usage is wired.
 */

export {
  buildCampaignsMessagingUsageViewModel as messagingUsageViewModelFromFixture,
  CAMPAIGNS_MESSAGING_BALANCES_FIXTURE as MESSAGING_USAGE_FIXTURE,
  CAMPAIGNS_MESSAGING_USAGE_COPY as MESSAGING_USAGE_COPY,
  type CampaignsMessagingBalancesFixture as MessagingUsageFixture,
  type CampaignsMessagingUsageViewModel as OperatorCampaignsMessagingUsageViewModel,
} from "@/lib/operatorCampaigns/campaignsMessagingCreditChrome"
