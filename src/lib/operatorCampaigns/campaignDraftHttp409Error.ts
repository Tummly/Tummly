/** Thrown when Draft PATCH returns HTTP 409 with a server message body. */
export class CampaignDraftHttp409Error extends Error {
  readonly status = 409 as const

  constructor(message: string) {
    super(message)
    this.name = "CampaignDraftHttp409Error"
  }
}
