const ACCOUNT_ALREADY_PROVISIONED_MESSAGES = new Set([
  "account already created.",
  "account already created",
  "user already exists.",
  "user already exists",
])

export function isAccountAlreadyProvisionedMessage(
  message: unknown
): boolean {
  if (typeof message !== "string") {
    return false
  }

  return ACCOUNT_ALREADY_PROVISIONED_MESSAGES.has(
    message.trim().toLowerCase()
  )
}
