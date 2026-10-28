import { describe, expect, it } from "vitest"

import {
  staffErrorMessage,
  staffStatusLabel,
  validateStaffPassword,
  validateStaffReason,
} from "./adminStaff"

describe("validateStaffPassword", () => {
  const email = "ops@tummly.com"

  it("accepts a twelve character password", () => {
    expect(validateStaffPassword("Abcdefghij12", email)).toBeNull()
  })

  it("refuses eleven characters", () => {
    expect(validateStaffPassword("Abcdefghij1", email)).toBe(
      "Use at least 12 characters."
    )
  })

  it("refuses more than 128 characters", () => {
    expect(validateStaffPassword("a".repeat(129), email)).toBe(
      "Use no more than 128 characters."
    )
  })

  it("refuses the development default passwords regardless of case", () => {
    expect(validateStaffPassword("Admin@123", email)).toBe(
      "Choose a password other than the Tummly development default."
    )
    expect(validateStaffPassword("support@123", email)).toBe(
      "Choose a password other than the Tummly development default."
    )
  })

  it("refuses a password equal to the email, ignoring case", () => {
    expect(validateStaffPassword("OPS@tummly.com", email)).toBe(
      "The password cannot be the same as the email address."
    )
  })
})

describe("validateStaffReason", () => {
  it("requires five to 500 characters after trimming", () => {
    expect(validateStaffReason("  abcd  ")).not.toBeNull()
    expect(validateStaffReason("abcde")).toBeNull()
    expect(validateStaffReason("r".repeat(500))).toBeNull()
    expect(validateStaffReason("r".repeat(501))).not.toBeNull()
  })
})

describe("staffStatusLabel", () => {
  it("shows Active for an enabled, unlocked account", () => {
    expect(staffStatusLabel({ isActive: true, isLocked: false })).toBe("Active")
  })

  it("shows Locked for an enabled account with a lock", () => {
    expect(staffStatusLabel({ isActive: true, isLocked: true })).toBe("Locked")
  })

  it("shows Disabled over Locked, because enabling does not clear the lock", () => {
    expect(staffStatusLabel({ isActive: false, isLocked: true })).toBe("Disabled")
  })
})

describe("staffErrorMessage", () => {
  it("maps duplicate email and guard codes to clear messages", () => {
    expect(staffErrorMessage("email_in_use", 409, "fallback")).toContain(
      "already belongs"
    )
    expect(staffErrorMessage("cannot_disable_self", 409, "fallback")).toContain(
      "your own"
    )
    expect(staffErrorMessage("last_active_admin", 409, "fallback")).toContain(
      "Another active Admin"
    )
  })

  it("reports permission denial for a 403 whatever the code", () => {
    expect(staffErrorMessage(null, 403, "fallback")).toBe(
      "You do not have permission to manage staff accounts."
    )
  })

  it("falls back for an unknown code", () => {
    expect(staffErrorMessage("something_new", 500, "Could not save.")).toBe(
      "Could not save."
    )
  })
})
