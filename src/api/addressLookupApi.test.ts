import { afterEach, describe, expect, it, vi } from "vitest"

vi.mock("@/api/axiosInstance", () => ({
  default: {
    get: vi.fn(),
  },
}))

import axiosInstance from "@/api/axiosInstance"
import {
  isAddressLookupAbortError,
  resolvePostcodeAddress,
  suggestAddresses,
} from "@/api/addressLookupApi"

const mockedGet = vi.mocked(axiosInstance.get)

describe("addressLookupApi", () => {
  afterEach(() => {
    mockedGet.mockReset()
  })

  it("detects aborted lookup requests", () => {
    expect(isAddressLookupAbortError(new DOMException("Aborted", "AbortError"))).toBe(
      true
    )
  })

  it("caches suggest results for the browser session", async () => {
    mockedGet.mockResolvedValue({
      data: {
        suggestions: [
          {
            id: "paf_1",
            label: "125 High Street, Manchester, M1 4AB",
          },
        ],
      },
    })

    const first = await suggestAddresses("125 High")
    const second = await suggestAddresses("  125 HIGH  ")

    expect(first).toHaveLength(1)
    expect(second).toEqual(first)
    expect(mockedGet).toHaveBeenCalledTimes(1)
  })

  it("caches postcode resolve results for the browser session", async () => {
    mockedGet.mockResolvedValue({
      data: {
        success: true,
        postcode: "M1 4AB",
        address: "125 High Street, Manchester",
        premises: [
          {
            address: "125 High Street, Manchester",
            postcode: "M1 4AB",
          },
        ],
        multiplePremises: false,
        usedBestMatch: false,
      },
    })

    const first = await resolvePostcodeAddress("M1 4AB", "125 High Street")
    const second = await resolvePostcodeAddress("M14AB", "125 High Street")

    expect(first?.address).toBe("125 High Street, Manchester")
    expect(second).toEqual(first)
    expect(mockedGet).toHaveBeenCalledTimes(1)
  })
})
