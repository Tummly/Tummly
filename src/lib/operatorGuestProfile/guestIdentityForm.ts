import { z } from "zod"

import { tryNormalizePhoneToE164 } from "@/lib/phoneNumber"

export const GUEST_IDENTITY_MAX_NAME_LENGTH = 150
export const GUEST_IDENTITY_MAX_CONTACT_LENGTH = 100

export type GuestIdentityDraft = {
  firstName: string
  lastName: string
  email: string
  phone: string
}

export type GuestIdentityFieldErrors = Partial<
  Record<keyof GuestIdentityDraft | "form", string>
>

export type GuestIdentityPatchPayload = {
  firstName: string
  lastName: string
  email: string | null
  phone: string | null
}

export function splitGuestName(name: string): {
  firstName: string
  lastName: string
} {
  const trimmed = name.trim()
  if (trimmed.length === 0) {
    return { firstName: "", lastName: "" }
  }

  const spaceIndex = trimmed.indexOf(" ")
  if (spaceIndex < 0) {
    return { firstName: trimmed, lastName: "" }
  }

  return {
    firstName: trimmed.slice(0, spaceIndex),
    lastName: trimmed.slice(spaceIndex + 1).trimStart(),
  }
}

export function joinGuestName(firstName: string, lastName: string): string {
  const first = firstName.trim()
  const last = lastName.trim()
  if (first.length === 0) {
    return last
  }
  if (last.length === 0) {
    return first
  }
  return `${first} ${last}`
}

function isValidEmail(value: string): boolean {
  return z.string().email().safeParse(value.trim()).success
}

export function validateGuestIdentityDraft(
  draft: GuestIdentityDraft
):
  | { ok: true; payload: GuestIdentityPatchPayload }
  | { ok: false; errors: GuestIdentityFieldErrors } {
  const firstName = draft.firstName.trim()
  const lastName = draft.lastName.trim()
  const email = draft.email.trim()
  const phone = draft.phone.trim()
  const joinedName = joinGuestName(firstName, lastName)
  const errors: GuestIdentityFieldErrors = {}

  if (joinedName.length === 0) {
    errors.firstName = "Name is required."
  } else if (joinedName.length > GUEST_IDENTITY_MAX_NAME_LENGTH) {
    errors.firstName = `Name must be at most ${GUEST_IDENTITY_MAX_NAME_LENGTH} characters.`
  }

  if (email.length > 0) {
    if (email.length > GUEST_IDENTITY_MAX_CONTACT_LENGTH) {
      errors.email = `Email must be at most ${GUEST_IDENTITY_MAX_CONTACT_LENGTH} characters.`
    } else if (!isValidEmail(email)) {
      errors.email = "Please enter a valid email address."
    }
  }

  if (phone.length > 0) {
    if (phone.length > GUEST_IDENTITY_MAX_CONTACT_LENGTH) {
      errors.phone = `Phone must be at most ${GUEST_IDENTITY_MAX_CONTACT_LENGTH} characters.`
    } else if (tryNormalizePhoneToE164(phone) == null) {
      errors.phone = "Please enter a valid UK phone number."
    }
  }

  if (email.length === 0 && phone.length === 0) {
    errors.form =
      "At least one contact channel (email or phone) is required."
  }

  if (Object.keys(errors).length > 0) {
    return { ok: false, errors }
  }

  return {
    ok: true,
    payload: {
      firstName,
      lastName,
      email: email.length === 0 ? null : email,
      phone: phone.length === 0 ? null : phone,
    },
  }
}
