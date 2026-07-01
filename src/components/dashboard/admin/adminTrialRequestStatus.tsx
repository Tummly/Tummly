import { Badge } from "@/components/ui/badge"
import type { AdminTrialRequest } from "@/types/admin"

type StatusBadgeVariant = "default" | "ready" | "error" | "secondary" | "outline"

function normalizeStatus(status: string) {
  return status.trim().toUpperCase().replace(/\s+/g, "_")
}

export function isDeclinedTrialRequest(request: AdminTrialRequest) {
  return normalizeStatus(request.status) === "DECLINED"
}

export function hasCreatedOperatorAccount(request: AdminTrialRequest) {
  return (
    request.isAccountCreated ||
    normalizeStatus(request.status) === "ACCOUNT_CREATED"
  )
}

export function canReviewTrialRequest(request: AdminTrialRequest) {
  return !request.isApproved && !isDeclinedTrialRequest(request)
}

export function getTrialRequestStatusDisplay(request: AdminTrialRequest): {
  label: string
  variant: StatusBadgeVariant
} {
  if (request.isAccountCreated || normalizeStatus(request.status) === "ACCOUNT_CREATED") {
    return { label: "Account created", variant: "ready" }
  }

  switch (normalizeStatus(request.status)) {
    case "DECLINED":
      return { label: "Declined", variant: "error" }
    case "MORE_INFO_REQUESTED":
      return { label: "More info requested", variant: "secondary" }
    case "APPROVED":
      return { label: "Approved", variant: "ready" }
    case "INVITE_SENT":
      return { label: "Invite sent", variant: "secondary" }
    case "EMAIL_VERIFIED":
      return { label: "Email verified", variant: "outline" }
    default:
      return {
        label: request.status || "Pending review",
        variant: "outline",
      }
  }
}

export function TrialRequestStatusBadge({ request }: { request: AdminTrialRequest }) {
  const { label, variant } = getTrialRequestStatusDisplay(request)

  return <Badge variant={variant}>{label}</Badge>
}

export function AccountTypeBadge({ accountType }: { accountType: string }) {
  const isMulti = accountType.toLowerCase() === "multi"

  return (
    <Badge variant={isMulti ? "secondary" : "outline"}>
      {isMulti ? "Multi-location" : "Single-location"}
    </Badge>
  )
}

export function ActivationStatusBadge({
  request,
}: {
  request: AdminTrialRequest
}) {
  if (!hasCreatedOperatorAccount(request)) {
    return null
  }

  const isActivated = request.activationStatus === "activated"

  return (
    <Badge variant={isActivated ? "ready" : "secondary"}>
      {isActivated ? "Activated" : "Not activated"}
    </Badge>
  )
}

export function getActivationStatusDetailLabel(
  detail: AdminTrialRequest["activationStatusDetail"]
) {
  switch (detail) {
    case "pending":
      return "Pending activation"
    case "active":
      return "Activated"
    case "expired":
      return "Activation expired"
    default:
      return "—"
  }
}
