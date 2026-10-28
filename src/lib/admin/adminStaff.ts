import type { AdminStaffMember } from "@/api/adminStaffApi"

/**
 * Mirrors backend StaffPasswordPolicy. The server is the authority; this
 * lets the dialogs show the rule before a request is sent.
 */
export const STAFF_PASSWORD_MIN_LENGTH = 12
/** BCrypt uses only the first 72 UTF-8 bytes; the server refuses longer passwords. */
export const STAFF_PASSWORD_MAX_BYTES = 72

const DEVELOPMENT_DEFAULT_PASSWORDS = ["admin@123", "support@123"]

export const STAFF_REASON_MIN_LENGTH = 5
export const STAFF_REASON_MAX_LENGTH = 500

/** Returns the first rule the password breaks, or null when it passes. */
export function validateStaffPassword(
  password: string,
  email?: string
): string | null {
  if (password.length === 0) {
    return `Use at least ${STAFF_PASSWORD_MIN_LENGTH} characters.`
  }
  if (DEVELOPMENT_DEFAULT_PASSWORDS.includes(password.toLowerCase())) {
    return "Choose a password other than the Tummly development default."
  }
  const trimmedEmail = email?.trim().toLowerCase() ?? ""
  if (trimmedEmail !== "" && password.toLowerCase() === trimmedEmail) {
    return "The password cannot be the same as the email address."
  }
  if (password.length < STAFF_PASSWORD_MIN_LENGTH) {
    return `Use at least ${STAFF_PASSWORD_MIN_LENGTH} characters.`
  }
  if (new TextEncoder().encode(password).length > STAFF_PASSWORD_MAX_BYTES) {
    return `Use no more than ${STAFF_PASSWORD_MAX_BYTES} bytes (about ${STAFF_PASSWORD_MAX_BYTES} plain characters).`
  }
  return null
}

export function validateStaffReason(reason: string): string | null {
  const length = reason.trim().length
  if (length < STAFF_REASON_MIN_LENGTH || length > STAFF_REASON_MAX_LENGTH) {
    return `Enter a reason of ${STAFF_REASON_MIN_LENGTH} to ${STAFF_REASON_MAX_LENGTH} characters.`
  }
  return null
}

export type StaffStatusLabel = "Active" | "Disabled" | "Locked"

/** Disabled wins over Locked: enabling an account does not clear its lock. */
export function staffStatusLabel(
  member: Pick<AdminStaffMember, "isActive" | "isLocked">
): StaffStatusLabel {
  if (!member.isActive) return "Disabled"
  if (member.isLocked) return "Locked"
  return "Active"
}

/** Maps a server code from the staff endpoints to a message for the user. */
export function staffErrorMessage(
  code: string | null,
  status: number | null,
  fallback: string
): string {
  if (status === 403) {
    return "You do not have permission to do this."
  }
  switch (code) {
    case "email_in_use":
      return "This email already belongs to a staff or operator account."
    case "cannot_disable_self":
      return "You cannot disable your own staff account."
    case "last_active_admin":
      return "Another active Admin must remain before this Admin can be disabled."
    case "cannot_reset_own_password":
      return "Use Change password to set your own password."
    case "current_password_invalid":
      return "The current password is not correct."
    case "new_password_same_as_current":
      return "Choose a password that is different from the current one."
    case "staff_not_found":
      return "This staff account no longer exists. Refresh the list."
    case "reason_invalid":
      return `Enter a reason of ${STAFF_REASON_MIN_LENGTH} to ${STAFF_REASON_MAX_LENGTH} characters.`
    case "password_too_short":
      return `Use at least ${STAFF_PASSWORD_MIN_LENGTH} characters.`
    case "password_too_long":
      return `Use no more than ${STAFF_PASSWORD_MAX_BYTES} bytes (about ${STAFF_PASSWORD_MAX_BYTES} plain characters).`
    case "too_many_attempts":
      return "Too many wrong current passwords. Try again in 15 minutes."
    case "password_development_default":
      return "Choose a password other than the Tummly development default."
    case "password_matches_email":
      return "The password cannot be the same as the email address."
    default:
      return fallback
  }
}
