import { AxiosError } from "axios"
import { beforeEach, describe, expect, it, vi } from "vitest"

vi.mock("@/api/dashboardApi", () => ({
  createDigitalGuestLink: vi.fn(),
}))

vi.mock("sonner", () => ({
  toast: {
    error: vi.fn(),
    success: vi.fn(),
  },
}))

import { createDigitalGuestLink as createDigitalGuestLinkApi } from "@/api/dashboardApi"
import { OPERATOR_CAPTURE_CREATE_DIGITAL_GUEST_LINK_COPY } from "@/lib/operatorCapture/capturePresentation"
import { createDigitalGuestLinkAdapters } from "@/lib/operatorCapture/createDigitalGuestLinkAdapters"
import { toast } from "sonner"

const createDigitalGuestLinkApiMock = vi.mocked(createDigitalGuestLinkApi)

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

const sampleInput = {
  linkName: "Instagram bio",
  channel: "SocialMedia" as const,
  status: "Active" as const,
  internalDescription: null,
}

describe("createDigitalGuestLinkAdapters", () => {
  beforeEach(() => {
    createDigitalGuestLinkApiMock.mockReset()
    vi.mocked(toast.error).mockReset()
    vi.mocked(toast.success).mockReset()
  })

  it("maps API success to ok true with qrCodeId", async () => {
    createDigitalGuestLinkApiMock.mockResolvedValue({
      success: true,
      qrCodeId: 55,
      qrType: "DigitalGuestLink",
      status: "Active",
      linkName: "Instagram bio",
      channel: "SocialMedia",
      internalDescription: null,
      qrLinkUrl: "https://example.test/scan/x",
    })

    const result = await createDigitalGuestLinkAdapters.createDigitalGuestLink(
      42,
      sampleInput
    )

    expect(createDigitalGuestLinkApiMock).toHaveBeenCalledWith(42, sampleInput)
    expect(result).toEqual({ ok: true, qrCodeId: 55 })
  })

  it("maps 409 with body message to duplicate_link_name", async () => {
    createDigitalGuestLinkApiMock.mockRejectedValue(
      axiosStatusError(409, { message: "Server duplicate message" })
    )

    const result = await createDigitalGuestLinkAdapters.createDigitalGuestLink(
      42,
      sampleInput
    )

    expect(result).toEqual({
      ok: false,
      reason: "duplicate_link_name",
      message: "Server duplicate message",
    })
  })

  it("maps 409 without body message to presentation fallback", async () => {
    createDigitalGuestLinkApiMock.mockRejectedValue(axiosStatusError(409, {}))

    const result = await createDigitalGuestLinkAdapters.createDigitalGuestLink(
      42,
      sampleInput
    )

    expect(result).toEqual({
      ok: false,
      reason: "duplicate_link_name",
      message:
        OPERATOR_CAPTURE_CREATE_DIGITAL_GUEST_LINK_COPY.linkNameDuplicate,
    })
  })

  it("maps non-409 failures to failed with failure toast copy", async () => {
    createDigitalGuestLinkApiMock.mockRejectedValue(axiosStatusError(500))

    const result = await createDigitalGuestLinkAdapters.createDigitalGuestLink(
      42,
      sampleInput
    )

    expect(result).toEqual({
      ok: false,
      reason: "failed",
      message: OPERATOR_CAPTURE_CREATE_DIGITAL_GUEST_LINK_COPY.failureToast,
    })
  })

  it("notifies create errors via toast.error", () => {
    createDigitalGuestLinkAdapters.onCreateDigitalGuestLinkError("boom")
    expect(toast.error).toHaveBeenCalledWith("boom")
  })

  it("notifies create success via toast.success", () => {
    createDigitalGuestLinkAdapters.onDigitalGuestLinkCreated("created")
    expect(toast.success).toHaveBeenCalledWith("created")
  })
})
