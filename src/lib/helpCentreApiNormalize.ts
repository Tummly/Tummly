import type {
  HelpCentreAuthorKind,
  HelpCentreQueryStatus,
} from "@/types/helpCentre"

const VALID_STATUSES = new Set<HelpCentreQueryStatus>([
  "NEW",
  "IN_PROGRESS",
  "WAITING_ON_CUSTOMER",
  "ESCALATED_TO_ADMIN",
  "RESOLVED",
  "CLOSED",
])

const VALID_AUTHOR_KINDS = new Set<HelpCentreAuthorKind>([
  "SUBMITTER",
  "SUPPORT",
  "OPERATOR",
])

export function parseHelpCentreQueryStatus(
  raw: unknown
): HelpCentreQueryStatus {
  const value = String(raw ?? "")

  if (VALID_STATUSES.has(value as HelpCentreQueryStatus)) {
    return value as HelpCentreQueryStatus
  }

  return "NEW"
}

export function parseHelpCentreAuthorKind(
  raw: unknown
): HelpCentreAuthorKind {
  const value = String(raw ?? "")

  if (VALID_AUTHOR_KINDS.has(value as HelpCentreAuthorKind)) {
    return value as HelpCentreAuthorKind
  }

  return "SUBMITTER"
}
