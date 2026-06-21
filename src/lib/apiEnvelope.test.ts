import { describe, expect, it } from "vitest"

import {
  getFetchErrorMessage,
  readBoolean,
  readFirstString,
  readNumber,
  readString,
  unwrapDataArray,
  unwrapDataObject,
} from "./apiEnvelope"

describe("readString", () => {
  it("matches keys case-insensitively", () => {
    expect(readString({ token: "a" }, "token")).toBe("a")
    expect(readString({ Token: "b" }, "token")).toBe("b")
    expect(readString({ TOKEN: "c" }, "token")).toBe("c")
  })

  it("returns null for missing or blank values", () => {
    expect(readString({}, "token")).toBeNull()
    expect(readString({ token: "   " }, "token")).toBeNull()
    expect(readString(null, "token")).toBeNull()
  })
})

describe("readFirstString", () => {
  it("returns the first alias match", () => {
    expect(
      readFirstString(
        { restaurantName: "Harbour" },
        ["businessName", "restaurantName", "groupName"]
      )
    ).toBe("Harbour")
  })

  it("returns null when no alias matches", () => {
    expect(
      readFirstString({}, ["businessName", "restaurantName"])
    ).toBeNull()
  })
})

describe("readBoolean", () => {
  it("reads booleans case-insensitively", () => {
    expect(readBoolean({ skipped: true }, "skipped")).toBe(true)
    expect(readBoolean({ Skipped: false }, "skipped")).toBe(false)
  })

  it("returns undefined when absent", () => {
    expect(readBoolean({}, "skipped")).toBeUndefined()
  })
})

describe("readNumber", () => {
  it("reads numbers case-insensitively", () => {
    expect(readNumber({ locationId: 12 }, "locationId")).toBe(12)
    expect(readNumber({ LocationId: 7 }, "locationId")).toBe(7)
  })

  it("returns null when absent", () => {
    expect(readNumber({}, "locationId")).toBeNull()
  })
})

describe("unwrapDataObject", () => {
  it("unwraps nested data objects", () => {
    expect(
      unwrapDataObject({
        success: true,
        data: { token: "jwt" },
      })
    ).toEqual({ token: "jwt" })
  })

  it("unwraps PascalCase data objects", () => {
    expect(
      unwrapDataObject({
        Success: true,
        Data: { Email: "owner@example.com" },
      })
    ).toEqual({ Email: "owner@example.com" })
  })

  it("returns flat root objects", () => {
    expect(
      unwrapDataObject({
        token: "jwt",
        accountType: "Single",
      })
    ).toEqual({
      token: "jwt",
      accountType: "Single",
    })
  })

  it("returns null for non-objects", () => {
    expect(unwrapDataObject(null)).toBeNull()
  })
})

describe("unwrapDataArray", () => {
  it("returns the data array when present", () => {
    expect(
      unwrapDataArray({
        success: true,
        data: [{ locationId: 1 }],
      })
    ).toEqual([{ locationId: 1 }])
  })

  it("returns an empty array when data is missing", () => {
    expect(unwrapDataArray({ success: true })).toEqual([])
    expect(unwrapDataArray(null)).toEqual([])
  })
})

describe("getFetchErrorMessage", () => {
  it("returns trimmed API messages", () => {
    expect(
      getFetchErrorMessage({ message: "  Invalid token.  " }, "Fallback")
    ).toBe("Invalid token.")
  })

  it("falls back when message is missing", () => {
    expect(getFetchErrorMessage({}, "Fallback")).toBe("Fallback")
  })
})
