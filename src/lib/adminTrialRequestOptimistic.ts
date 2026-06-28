import type { AdminTrialRequest } from "@/types/admin"

function withTimestampFields(
  request: AdminTrialRequest,
  patch: Partial<AdminTrialRequest>
): AdminTrialRequest {
  return { ...request, ...patch }
}

function nowIso() {
  return new Date().toISOString()
}

export function applyTrialRequestDelete(
  requests: AdminTrialRequest[],
  requestId: number
) {
  return requests.filter((request) => request.id !== requestId)
}

export function applyTrialRequestApprove(
  requests: AdminTrialRequest[],
  requestId: number
) {
  const now = nowIso()

  return requests.map((request) =>
    request.id === requestId
      ? withTimestampFields(request, {
          isApproved: true,
          status: "APPROVED",
          approvedAt: now,
          reviewedAt: now,
          reviewedBy: "Admin",
          inviteSentAt: now,
        })
      : request
  )
}

export function applyTrialRequestDecline(
  requests: AdminTrialRequest[],
  requestId: number,
  declineReason: string
) {
  const now = nowIso()

  return requests.map((request) =>
    request.id === requestId
      ? withTimestampFields(request, {
          status: "DECLINED",
          declinedAt: now,
          declineReason,
          reviewedAt: now,
          reviewedBy: "Admin",
        })
      : request
  )
}

export function applyTrialRequestMoreInfo(
  requests: AdminTrialRequest[],
  requestId: number,
  moreInfoMessage: string
) {
  const now = nowIso()

  return requests.map((request) =>
    request.id === requestId
      ? withTimestampFields(request, {
          status: "MORE_INFO_REQUESTED",
          moreInfoRequestedAt: now,
          moreInfoMessage,
          reviewedAt: now,
          reviewedBy: "Admin",
        })
      : request
  )
}

export function applyTrialRequestResendInvite(
  requests: AdminTrialRequest[],
  requestId: number
) {
  const now = nowIso()

  return requests.map((request) =>
    request.id === requestId
      ? withTimestampFields(request, {
          inviteSentAt: now,
        })
      : request
  )
}
