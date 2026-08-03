import { AxiosError } from "axios"
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest"

vi.mock("@/api/dashboardApi", () => ({
  prepareFeedbackRecoveryDraft: vi.fn(),
}))

import { prepareFeedbackRecoveryDraft as prepareFeedbackRecoveryDraftApi } from "@/api/dashboardApi"
import { prepareRecoveryDraft } from "./prepareRecoveryDraft"
import type { PrepareFeedbackRecoveryDraftRequest } from "@/types/dashboard"

const prepareFeedbackRecoveryDraftApiMock = vi.mocked(
  prepareFeedbackRecoveryDraftApi
)

function axiosStatusError(status: number, data?: unknown): AxiosError {
  return new AxiosError(
    "Request failed",
    undefined,
    undefined,
    undefined,
    {
      status,
      statusText: "Error",
      headers: {},
      config: {} as never,
      data,
    }
  )
}

const sampleBody: PrepareFeedbackRecoveryDraftRequest = {
  channel: "email",
  purpose: "acknowledge_and_apologise",
  tone: "warm",
  includeNotes: null,
  mode: "prepare",
  currentBody: null,
  currentSubject: null,
}

describe("prepareRecoveryDraft", () => {
  beforeEach(() => {
    prepareFeedbackRecoveryDraftApiMock.mockReset()
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  it("forwards feedbackId and body, resolving to a succeeded outcome", async () => {
    prepareFeedbackRecoveryDraftApiMock.mockResolvedValue({
      success: true,
      body: "Draft body",
      subject: "Draft subject",
      channel: "email",
    })

    const result = await prepareRecoveryDraft(2418, sampleBody)

    expect(prepareFeedbackRecoveryDraftApiMock).toHaveBeenCalledWith(
      2418,
      sampleBody,
      expect.any(AbortSignal)
    )
    expect(result).toEqual({
      status: "succeeded",
      body: "Draft body",
      subject: "Draft subject",
      channel: "email",
    })
  })

  it("defaults a missing subject to null on success", async () => {
    prepareFeedbackRecoveryDraftApiMock.mockResolvedValue({
      success: true,
      body: "Draft body",
      channel: "sms",
    })

    const result = await prepareRecoveryDraft(2418, sampleBody)

    expect(result).toEqual({
      status: "succeeded",
      body: "Draft body",
      subject: null,
      channel: "sms",
    })
  })

  it("treats a missing body or channel as a failure even when success is true", async () => {
    prepareFeedbackRecoveryDraftApiMock.mockResolvedValue({ success: true })

    const result = await prepareRecoveryDraft(2418, sampleBody)

    expect(result).toEqual({ status: "failed", retryable: true })
  })

  it("honours an explicit retryable: false on an unsuccessful response", async () => {
    prepareFeedbackRecoveryDraftApiMock.mockResolvedValue({
      success: false,
      retryable: false,
    })

    const result = await prepareRecoveryDraft(2418, sampleBody)

    expect(result).toEqual({ status: "failed", retryable: false })
  })

  it("classifies a cancelled request (ERR_CANCELED) as a retryable failure", async () => {
    const cancelError = new AxiosError("canceled")
    cancelError.code = "ERR_CANCELED"
    prepareFeedbackRecoveryDraftApiMock.mockRejectedValue(cancelError)

    const result = await prepareRecoveryDraft(2418, sampleBody)

    expect(result).toEqual({ status: "failed", retryable: true })
  })

  it("classifies a 502 response using the server's retryable flag", async () => {
    prepareFeedbackRecoveryDraftApiMock.mockRejectedValue(
      axiosStatusError(502, { retryable: false })
    )

    const result = await prepareRecoveryDraft(2418, sampleBody)

    expect(result).toEqual({ status: "failed", retryable: false })
  })

  it("defaults a 502 response with no retryable flag to retryable", async () => {
    prepareFeedbackRecoveryDraftApiMock.mockRejectedValue(axiosStatusError(502))

    const result = await prepareRecoveryDraft(2418, sampleBody)

    expect(result).toEqual({ status: "failed", retryable: true })
  })

  it("classifies unrecognised failures as retryable", async () => {
    prepareFeedbackRecoveryDraftApiMock.mockRejectedValue(
      new Error("network down")
    )

    const result = await prepareRecoveryDraft(2418, sampleBody)

    expect(result).toEqual({ status: "failed", retryable: true })
  })

  it("re-throws instead of swallowing errors once the caller's signal is aborted", async () => {
    const controller = new AbortController()
    const abortError = new DOMException("Aborted", "AbortError")
    prepareFeedbackRecoveryDraftApiMock.mockImplementation(async () => {
      controller.abort()
      throw abortError
    })

    await expect(
      prepareRecoveryDraft(2418, sampleBody, controller.signal)
    ).rejects.toBe(abortError)
  })

  it("aborts the in-flight request once the caller's signal aborts", () => {
    vi.useFakeTimers()
    const controller = new AbortController()
    let capturedSignal: AbortSignal | undefined
    prepareFeedbackRecoveryDraftApiMock.mockImplementation(
      (_feedbackId, _body, signal) => {
        capturedSignal = signal
        return new Promise(() => {
          /* left pending to observe the abort forwarding */
        })
      }
    )

    void prepareRecoveryDraft(2418, sampleBody, controller.signal)
    expect(capturedSignal?.aborted).toBe(false)

    controller.abort()
    expect(capturedSignal?.aborted).toBe(true)

    vi.clearAllTimers()
  })

  it("aborts the in-flight request once the 60s client-side timeout elapses", async () => {
    vi.useFakeTimers()
    let capturedSignal: AbortSignal | undefined
    prepareFeedbackRecoveryDraftApiMock.mockImplementation(
      (_feedbackId, _body, signal) =>
        new Promise((_resolve, reject) => {
          capturedSignal = signal
          signal?.addEventListener("abort", () => {
            reject(new DOMException("Aborted", "AbortError"))
          })
        })
    )

    const resultPromise = prepareRecoveryDraft(2418, sampleBody)
    await vi.advanceTimersByTimeAsync(60_000)

    expect(capturedSignal?.aborted).toBe(true)
    await expect(resultPromise).resolves.toEqual({
      status: "failed",
      retryable: true,
    })
  })
})
