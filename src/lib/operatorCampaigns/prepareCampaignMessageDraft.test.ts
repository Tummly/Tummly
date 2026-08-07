import { AxiosError } from "axios"
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest"

vi.mock("@/api/dashboardApi", () => ({
  prepareCampaignMessageDraft: vi.fn(),
}))

import { prepareCampaignMessageDraft as prepareCampaignMessageDraftApi } from "@/api/dashboardApi"
import { prepareCampaignMessageDraft } from "./prepareCampaignMessageDraft"
import type { PrepareCampaignMessageDraftRequest } from "@/lib/operatorCampaigns/createCampaignWizardModule"

const prepareCampaignMessageDraftApiMock = vi.mocked(
  prepareCampaignMessageDraftApi
)

function axiosStatusError(status: number, data?: unknown): AxiosError {
  return new AxiosError("Request failed", undefined, undefined, undefined, {
    status,
    statusText: "Error",
    headers: {},
    config: {} as never,
    data,
  })
}

const sampleRequest: PrepareCampaignMessageDraftRequest = {
  locationId: 42,
  channel: "email",
  goalId: "thank-recent-guests",
  audienceKey: "all-eligible-guests",
  offerStance: "no-offer",
  campaignName: null,
  tone: "friendly_and_clear",
  includeNotes: null,
  mode: "prepare",
  currentBody: null,
  currentSubject: null,
}

describe("prepareCampaignMessageDraft", () => {
  beforeEach(() => {
    prepareCampaignMessageDraftApiMock.mockReset()
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  it("forwards the request body and resolves a succeeded outcome", async () => {
    prepareCampaignMessageDraftApiMock.mockResolvedValue({
      success: true,
      body: "Draft body",
      subject: "Draft subject",
      channel: "email",
    })

    const result = await prepareCampaignMessageDraft(sampleRequest)

    expect(prepareCampaignMessageDraftApiMock).toHaveBeenCalledWith(
      expect.objectContaining({
        locationId: 42,
        channel: "email",
        goalId: "thank-recent-guests",
        mode: "prepare",
      }),
      expect.any(AbortSignal)
    )
    expect(result).toEqual({
      status: "succeeded",
      body: "Draft body",
      subject: "Draft subject",
      channel: "email",
    })
  })

  it("maps a failed envelope to a retryable failure", async () => {
    prepareCampaignMessageDraftApiMock.mockResolvedValue({
      success: false,
      message: "We could not prepare a draft.",
      retryable: true,
    })

    const result = await prepareCampaignMessageDraft(sampleRequest)

    expect(result).toEqual({ status: "failed", retryable: true })
  })

  it("maps HTTP 502 to a failed outcome", async () => {
    prepareCampaignMessageDraftApiMock.mockRejectedValue(
      axiosStatusError(502, { success: false, retryable: true })
    )

    const result = await prepareCampaignMessageDraft(sampleRequest)

    expect(result).toEqual({ status: "failed", retryable: true })
  })

  it("maps rewrite mode through to the API body", async () => {
    prepareCampaignMessageDraftApiMock.mockResolvedValue({
      success: true,
      body: "Rewritten body",
      subject: "Prior subject",
      channel: "email",
    })

    await prepareCampaignMessageDraft({
      ...sampleRequest,
      mode: "rewrite_message",
      currentBody: "Prior body",
      currentSubject: "Prior subject",
    })

    expect(prepareCampaignMessageDraftApiMock).toHaveBeenCalledWith(
      expect.objectContaining({
        mode: "rewrite_message",
        currentBody: "Prior body",
        currentSubject: "Prior subject",
      }),
      expect.any(AbortSignal)
    )
  })
})
