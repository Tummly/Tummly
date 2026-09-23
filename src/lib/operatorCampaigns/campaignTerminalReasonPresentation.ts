/**
 * Campaign terminal reason codes — written at fire close; mapped for Needs attention.
 * Stable API ids; copy lives here (and Assistant twin).
 */

export const CAMPAIGN_TERMINAL_REASON_IDS = [
  "soft-locked",
  "workspace-paused",
  "location-inactive",
  "location-missing",
  "channel-missing",
  "eligibility-invalid",
  "zero-eligible",
  "credit-hold-exhausted",
  "no-accepted-sends",
  "settle-failed",
  "close-failed",
  "mid-send-stop",
] as const

export type CampaignTerminalReasonId =
  (typeof CAMPAIGN_TERMINAL_REASON_IDS)[number]

const REASON_SET = new Set<string>(CAMPAIGN_TERMINAL_REASON_IDS)

export function parseCampaignTerminalReasonId(
  value: string | null | undefined
): CampaignTerminalReasonId | null {
  if (value == null) {
    return null
  }
  const trimmed = value.trim()
  if (!REASON_SET.has(trimmed)) {
    return null
  }
  return trimmed as CampaignTerminalReasonId
}

const FAILED_REASON_COPY: Record<CampaignTerminalReasonId, string> = {
  "soft-locked":
    "This campaign failed because the account was soft-locked at send time.",
  "workspace-paused":
    "This campaign failed because the workspace was paused at send time.",
  "location-inactive":
    "This campaign failed because the location was not active at send time.",
  "location-missing":
    "This campaign failed because the campaign location was not found.",
  "channel-missing":
    "This campaign failed because no send channel was set.",
  "eligibility-invalid":
    "This campaign failed because audience eligibility could not be evaluated.",
  "zero-eligible":
    "This campaign failed because no frozen recipients were still eligible.",
  "credit-hold-exhausted":
    "This campaign failed because the reserved credit hold ran out before any send was accepted.",
  "no-accepted-sends":
    "This campaign failed because no messages were accepted by the provider.",
  "settle-failed":
    "This campaign failed while settling reserved credits.",
  "close-failed":
    "This campaign failed while closing the credit hold.",
  "mid-send-stop":
    "This campaign failed after send work stopped early.",
}

const PARTIAL_REASON_COPY: Record<CampaignTerminalReasonId, string> = {
  "soft-locked":
    "This campaign was only partially sent; the account was soft-locked.",
  "workspace-paused":
    "This campaign was only partially sent; the workspace was paused.",
  "location-inactive":
    "This campaign was only partially sent; the location was not active.",
  "location-missing":
    "This campaign was only partially sent; the campaign location was not found.",
  "channel-missing":
    "This campaign was only partially sent; no send channel was set.",
  "eligibility-invalid":
    "This campaign was only partially sent; audience eligibility could not be evaluated.",
  "zero-eligible":
    "This campaign was only partially sent; remaining recipients were no longer eligible.",
  "credit-hold-exhausted":
    "This campaign was only partially sent because the reserved credit hold ran out.",
  "no-accepted-sends":
    "This campaign was only partially sent; later messages were not accepted.",
  "settle-failed":
    "This campaign was only partially sent; settling reserved credits failed.",
  "close-failed":
    "This campaign was only partially sent; closing the credit hold failed.",
  "mid-send-stop":
    "This campaign was only partially sent; send work stopped before all recipients.",
}

const GENERIC_FAILED = "This campaign failed."
const GENERIC_PARTIAL = "This campaign was only partially sent."

/** Needs attention body — prefer terminal reason copy; fall back to generic status copy. */
export function campaignNeedsAttentionBody(input: {
  status: "failed" | "partially-sent"
  terminalReason?: string | null
}): string {
  const reason = parseCampaignTerminalReasonId(input.terminalReason)
  if (reason == null) {
    return input.status === "failed" ? GENERIC_FAILED : GENERIC_PARTIAL
  }
  return input.status === "failed"
    ? FAILED_REASON_COPY[reason]
    : PARTIAL_REASON_COPY[reason]
}
