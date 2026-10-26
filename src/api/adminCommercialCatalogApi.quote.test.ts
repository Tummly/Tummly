import { afterEach, describe, expect, it, vi } from "vitest"

vi.mock("@/api/axiosInstance", () => ({
  default: {
    post: vi.fn(),
  },
}))

import axiosInstance from "@/api/axiosInstance"
import { quotePublicCustomPlan } from "@/api/adminCommercialCatalogApi"

const mockedPost = vi.mocked(axiosInstance.post)

const REQUEST = {
  cadence: "monthly" as const,
  basePlan: "starter" as const,
  locations: 2.5,
  packs: {},
}

describe("quotePublicCustomPlan", () => {
  afterEach(() => {
    mockedPost.mockReset()
  })

  it("keeps the error code from a 400 response body", async () => {
    mockedPost.mockRejectedValueOnce(
      Object.assign(new Error("Request failed with status code 400"), {
        isAxiosError: true,
        response: {
          status: 400,
          data: {
            success: false,
            code: "invalid_location_count",
            message: "Enter a whole number of Locations.",
          },
        },
      }),
    )

    await expect(quotePublicCustomPlan(REQUEST)).rejects.toMatchObject({
      code: "invalid_location_count",
      message: "Enter a whole number of Locations.",
    })
  })

  it("rethrows a network failure unchanged", async () => {
    const networkError = Object.assign(new Error("Network Error"), {
      isAxiosError: true,
      response: undefined,
    })
    mockedPost.mockRejectedValueOnce(networkError)

    await expect(quotePublicCustomPlan(REQUEST)).rejects.toBe(networkError)
  })

  it("maps quoteHash and vatRateBps from a 200 response", async () => {
    mockedPost.mockResolvedValueOnce({
      data: {
        success: true,
        item: {
          catalogPublicId: "catalog-1",
          locations: 1,
          quoteHash: "b".repeat(32),
          vatRateBps: 2000,
        },
      },
    })

    const quote = await quotePublicCustomPlan({ ...REQUEST, locations: 1 })
    expect(quote.quoteHash).toBe("b".repeat(32))
    expect(quote.vatRateBps).toBe(2000)
  })
})
