import { describe, expect, it } from "vitest"

import {
  validateGuestIdentityDraft,
  type GuestIdentityDraft,
} from "@/lib/operatorGuestProfile/guestIdentityForm"

function createDraft(
  overrides: Partial<GuestIdentityDraft> = {}
): GuestIdentityDraft {
  return {
    firstName: "Amelia",
    lastName: "Hart",
    email: "",
    phone: "",
    emailReplacing: false,
    phoneReplacing: false,
    emailMasked: "a••••@example.com",
    phoneMasked: "••••0456",
    ...overrides,
  }
}

describe("validateGuestIdentityDraft", () => {
  it("keeps masked contacts when neither field is replaced", () => {
    const result = validateGuestIdentityDraft(createDraft())

    expect(result).toEqual({
      ok: true,
      payload: {
        firstName: "Amelia",
        lastName: "Hart",
        replaceEmail: false,
        email: null,
        replacePhone: false,
        phone: null,
      },
    })
  })

  it("treats a not-replaced email as present when its masked value is set", () => {
    const result = validateGuestIdentityDraft(
      createDraft({ emailMasked: "a••••@example.com", phoneMasked: "" })
    )

    expect(result.ok).toBe(true)
  })

  it("requires a contact when neither masked value exists and nothing is replaced", () => {
    const result = validateGuestIdentityDraft(
      createDraft({ emailMasked: "", phoneMasked: "" })
    )

    expect(result.ok).toBe(false)
    if (!result.ok) {
      expect(result.errors.form).toMatch(/contact/i)
    }
  })

  it("sends a replaced email and ignores the masked value", () => {
    const result = validateGuestIdentityDraft(
      createDraft({
        emailReplacing: true,
        email: "  New@Example.com ",
      })
    )

    expect(result).toEqual({
      ok: true,
      payload: {
        firstName: "Amelia",
        lastName: "Hart",
        replaceEmail: true,
        email: "New@Example.com",
        replacePhone: false,
        phone: null,
      },
    })
  })

  it("reports an invalid replaced email", () => {
    const result = validateGuestIdentityDraft(
      createDraft({ emailReplacing: true, email: "bob" })
    )

    expect(result.ok).toBe(false)
    if (!result.ok) {
      expect(result.errors.email).toBe("Please enter a valid email address.")
    }
  })

  it("reports an invalid replaced UK phone", () => {
    const result = validateGuestIdentityDraft(
      createDraft({ phoneReplacing: true, phone: "12345" })
    )

    expect(result.ok).toBe(false)
    if (!result.ok) {
      expect(result.errors.phone).toMatch(/UK phone/i)
    }
  })

  it("clears a replaced email when the masked phone keeps the guest reachable", () => {
    const result = validateGuestIdentityDraft(
      createDraft({ emailReplacing: true, email: "" })
    )

    expect(result).toEqual({
      ok: true,
      payload: {
        firstName: "Amelia",
        lastName: "Hart",
        replaceEmail: true,
        email: null,
        replacePhone: false,
        phone: null,
      },
    })
  })

  it("rejects replacing both contacts with blank values", () => {
    const result = validateGuestIdentityDraft(
      createDraft({ emailReplacing: true, phoneReplacing: true })
    )

    expect(result.ok).toBe(false)
    if (!result.ok) {
      expect(result.errors.form).toMatch(/contact/i)
    }
  })
})
