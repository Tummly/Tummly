/** Thrown when Campaign commit returns HTTP 503 billing_reserve_unavailable. */
export class CampaignBillingReserveUnavailableError extends Error {
  readonly code = "billing_reserve_unavailable" as const
  readonly status = 503 as const

  constructor(message: string) {
    super(message)
    this.name = "CampaignBillingReserveUnavailableError"
  }
}

export function isCampaignBillingReserveUnavailableError(
  error: unknown
): error is CampaignBillingReserveUnavailableError {
  return error instanceof CampaignBillingReserveUnavailableError
}
