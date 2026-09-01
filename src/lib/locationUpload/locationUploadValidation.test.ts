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
        city: "Manchester",
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
        city: "Manchester",
        postcode: "M1 4AB",
        addressOverridden: false,
        locationPhone: "",
        localContact: "",
      })
    ).toBe("missing_required")
  })

  it("fills city from the last address segment when City is empty", () => {
    expect(
      getUploadedLocationStatus({
        locationName: "Bistro",
        address: "125 High Street, Manchester",
        city: "",
        postcode: "M1 4AB",
        addressOverridden: false,
        locationPhone: "",
        localContact: "",
      })
    ).toBe("ready")
  })

  it("marks missing City as missing required when address has no town", () => {
    expect(
      getUploadedLocationStatus({
        locationName: "Bistro",
        address: "125 High Street",
        city: "",
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
        city: "Manchester",
        postcode: "not-valid",
        addressOverridden: false,
        locationPhone: "",
        localContact: "",
      })
    ).toBe("invalid_postcode")
  })

  it("marks invalid location phones", () => {
    expect(
      getUploadedLocationStatus({
        locationName: "Bistro",
        address: "125 High Street",
        city: "Manchester",
        postcode: "M1 4AB",
        addressOverridden: false,
        locationPhone: "123",
        localContact: "",
      })
    ).toBe("invalid_phone")
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
    expect(getUploadedLocationStatusLabel("invalid_phone")).toBe(
      "Please enter a valid UK phone number."
    )
  })
})
