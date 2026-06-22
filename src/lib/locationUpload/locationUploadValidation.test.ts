import {
  combineLocalContact,
  getUploadedLocationStatus,
  getUploadedLocationStatusLabel,
} from "@/lib/locationUpload/locationUploadValidation"
import { describe, expect, it } from "vitest"

describe("combineLocalContact", () => {
  it("combines name and email with an em dash", () => {
    expect(combineLocalContact("Jane Smith", "jane@bistro.com")).toBe(
      "Jane Smith — jane@bistro.com"
    )
  })

  it("returns name only when email is empty", () => {
    expect(combineLocalContact("Jane Smith", "")).toBe("Jane Smith")
  })

  it("returns email only when name is empty", () => {
    expect(combineLocalContact("", "jane@bistro.com")).toBe("jane@bistro.com")
  })
})

describe("getUploadedLocationStatus", () => {
  it("marks complete rows as ready", () => {
    expect(
      getUploadedLocationStatus({
        locationName: "Bistro",
        address: "125 High Street",
        postcode: "M1 4AB",
        addressOverridden: false,
        locationPhone: "",
        localContact: "",
      })
    ).toBe("ready")
  })

  it("marks missing required values", () => {
    expect(
      getUploadedLocationStatus({
        locationName: "",
        address: "125 High Street",
        postcode: "M1 4AB",
        addressOverridden: false,
        locationPhone: "",
        localContact: "",
      })
    ).toBe("missing_required")
  })

  it("marks invalid postcodes", () => {
    expect(
      getUploadedLocationStatus({
        locationName: "Bistro",
        address: "125 High Street",
        postcode: "not-valid",
        addressOverridden: false,
        locationPhone: "",
        localContact: "",
      })
    ).toBe("invalid_postcode")
  })
})

describe("getUploadedLocationStatusLabel", () => {
  it("returns user-facing labels", () => {
    expect(getUploadedLocationStatusLabel("ready")).toBe("Ready")
    expect(getUploadedLocationStatusLabel("missing_required")).toBe(
      "Missing required field"
    )
    expect(getUploadedLocationStatusLabel("invalid_postcode")).toBe(
      "Please enter a valid UK postcode"
    )
  })
})
