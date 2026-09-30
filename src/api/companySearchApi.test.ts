import { afterEach, describe, expect, it, vi } from "vitest"

vi.mock("@/api/axiosInstance", () => ({
  default: {
    get: vi.fn(),
  },
}))

import axiosInstance from "@/api/axiosInstance"
import {
  isCompanySearchAbortError,
  suggestCompanies,
} from "@/api/companySearchApi"

const mockedGet = vi.mocked(axiosInstance.get)

describe("companySearchApi", () => {
  afterEach(() => {
    mockedGet.mockReset()
  })

  it("detects aborted company search requests", () => {
    expect(
      isCompanySearchAbortError(new DOMException("Aborted", "AbortError"))
    ).toBe(true)
  })

  it("caches suggest results for the browser session", async () => {
    mockedGet.mockResolvedValue({
      data: {
        suggestions: [
          {
            companyNumber: "12345678",
            title: "ACME HOSPITALITY LTD",
            addressSnippet: "1 High Street, London",
            companyStatus: "active",
          },
        ],
      },
    })

    const first = await suggestCompanies("acme")
    const second = await suggestCompanies("  ACME  ")

    expect(first).toHaveLength(1)
    expect(second).toEqual(first)
    expect(mockedGet).toHaveBeenCalledTimes(1)
    expect(mockedGet).toHaveBeenCalledWith("/companies/suggest", {
      params: { q: "acme" },
      signal: undefined,
    })
  })

  it("returns empty suggestions for short queries", async () => {
    const result = await suggestCompanies("acm")

    expect(result).toEqual([])
    expect(mockedGet).not.toHaveBeenCalled()
  })
})
