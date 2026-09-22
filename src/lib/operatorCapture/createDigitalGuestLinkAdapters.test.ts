import { AxiosError } from "axios"
import { beforeEach, describe, expect, it, vi } from "vitest"

vi.mock("@/api/dashboardApi", () => ({
  createDigitalGuestLink: vi.fn(),
  listCatalogOffers: vi.fn(),
}))

vi.mock("sonner", () => ({
  toast: {
    error: vi.fn(),
    success: vi.fn(),
  },
}))

import {
  createDigitalGuestLink as createDigitalGuestLinkApi,
  listCatalogOffers as listCatalogOffersApi,
} from "@/api/dashboardApi"
import { OPERATOR_CAPTURE_CREATE_DIGITAL_GUEST_LINK_COPY } from "@/lib/operatorCapture/capturePresentation"
import { createDigitalGuestLinkAdapters } from "@/lib/operatorCapture/createDigitalGuestLinkAdapters"
import { toast } from "sonner"

const createDigitalGuestLinkApiMock = vi.mocked(createDigitalGuestLinkApi)
const listCatalogOffersApiMock = vi.mocked(listCatalogOffersApi)

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
    listCatalogOffersApiMock.mockReset()
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

  it("strips locationId and connectedOfferId from create API body", async () => {
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

    await createDigitalGuestLinkAdapters.createDigitalGuestLink(42, {
      ...sampleInput,
      locationId: 99,
      connectedOfferId: 88,
    })

    expect(createDigitalGuestLinkApiMock).toHaveBeenCalledWith(42, sampleInput)
  })

  it("lists attachable draft and active offers for connected offer dropdown", async () => {
    listCatalogOffersApiMock.mockResolvedValue({
      success: true,
      items: [
        {
          id: 1,
          locationId: 42,
          title: "Draft treat",
          status: "draft",
          offerType: "percent",
          validity: "always",
          expiryDate: null,
          attachKinds: [],
          createdAt: "2026-01-01T00:00:00.000Z",
          updatedAt: "2026-01-01T00:00:00.000Z",
        },
        {
          id: 2,
          locationId: 42,
          title: "Active treat",
          status: "active",
          offerType: "percent",
          validity: "always",
          expiryDate: null,
          attachKinds: [],
          createdAt: "2026-01-01T00:00:00.000Z",
          updatedAt: "2026-01-01T00:00:00.000Z",
        },
        {
          id: 3,
          locationId: 42,
          title: "Paused treat",
          status: "paused",
          offerType: "percent",
          validity: "always",
          expiryDate: null,
          attachKinds: [],
          createdAt: "2026-01-01T00:00:00.000Z",
          updatedAt: "2026-01-01T00:00:00.000Z",
        },
      ],
      totalCount: 3,
      page: 1,
      pageSize: 25,
      tabCounts: {
        all: 3,
        needsAttention: 0,
        drafts: 1,
        inFlight: 0,
        sent: 0,
      },
    })

    const options =
      await createDigitalGuestLinkAdapters.listConnectedOfferOptions(42)

    expect(listCatalogOffersApiMock).toHaveBeenCalledWith({
      locationId: 42,
      view: "all",
      page: 1,
      pageSize: 25,
      status: ["draft", "active"],
    })
    expect(options).toEqual([
      { id: 1, title: "Draft treat" },
      { id: 2, title: "Active treat" },
    ])
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
