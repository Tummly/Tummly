import { isAxiosError } from "axios"

import type { PlanEntitlementsAccountSnapshot } from "@/lib/planEntitlements/planEntitlementsPresentation"
import {
  normalizePlanEntitlementsAccount,
  teamMemberCapReachedMessage,
} from "@/lib/planEntitlements/planEntitlementsPresentation"
import axiosInstance from "@/api/axiosInstance"
import type {
  AccessActivityList,
  TeamInviteDraft,
  TeamPermissionsPageData,
} from "@/lib/operatorTeamPermissions/createOperatorTeamPermissionsPageModule"

function readApiError(error: unknown, fallback: string): string {
  if (isAxiosError(error)) {
    const payload = error.response?.data as
      | {
          message?: unknown
          code?: unknown
          cap?: unknown
          current?: unknown
        }
      | undefined
    if (
      payload?.code === "team_member_cap_reached"
      && typeof payload.cap === "number"
    ) {
      return teamMemberCapReachedMessage({
        cap: payload.cap,
        current:
          typeof payload.current === "number" ? payload.current : payload.cap,
        atCap: true,
        available: true,
      })
    }
    const message = payload?.message
    if (typeof message === "string" && message.trim() !== "") {
      return message
    }
  }
  if (error instanceof Error && error.message.trim() !== "") {
    return error.message
  }
  return fallback
}

function rethrow(error: unknown, fallback: string): never {
  throw new Error(readApiError(error, fallback))
}

export async function getTeamPermissionsPage(): Promise<TeamPermissionsPageData> {
  const { data } = await axiosInstance.get<TeamPermissionsPageData>(
    "/team-permissions"
  )
  return {
    ...data,
    matrix: data.matrix ?? [],
    invitations: data.invitations ?? [],
    entitlements: normalizePlanEntitlementsAccount(
      data.entitlements as Record<string, unknown> | undefined
    ),
  }
}

export async function updateTeamMemberRole(
  membershipId: number,
  permissionRole: string
): Promise<void> {
  await axiosInstance.patch(`/team-permissions/members/${membershipId}/role`, {
    permissionRole,
  })
}

export async function updateTeamMemberLocationScope(
  membershipId: number,
  payload: { locationScope: "all" | "named"; namedLocationIds: number[] }
): Promise<void> {
  await axiosInstance.patch(
    `/team-permissions/members/${membershipId}/location-scope`,
    payload
  )
}

export async function deactivateTeamMember(
  membershipId: number
): Promise<void> {
  await axiosInstance.post(
    `/team-permissions/members/${membershipId}/deactivate`
  )
}

export async function reactivateTeamMember(
  membershipId: number
): Promise<void> {
  await axiosInstance.post(
    `/team-permissions/members/${membershipId}/reactivate`
  )
}

export async function removeTeamMember(membershipId: number): Promise<void> {
  await axiosInstance.delete(`/team-permissions/members/${membershipId}`)
}

export async function saveTeamPermissionsMatrix(
  adminCells: Array<{ areaId: string; level: string }>
): Promise<void> {
  await axiosInstance.put("/team-permissions/matrix", { adminCells })
}

export async function sendTeamInvitation(
  payload: TeamInviteDraft
): Promise<void> {
  try {
    await axiosInstance.post("/team-permissions/invitations", {
      email: payload.email,
      fullName: payload.fullName,
      permissionRole: payload.permissionRole,
      locationScope: payload.locationScope,
      namedLocationIds: payload.namedLocationIds,
      message: payload.message.trim() === "" ? null : payload.message,
    })
  } catch (error) {
    rethrow(error, "Could not send invite.")
  }
}

export async function resendTeamInvitation(
  invitationId: number
): Promise<void> {
  try {
    await axiosInstance.post(
      `/team-permissions/invitations/${invitationId}/resend`
    )
  } catch (error) {
    rethrow(error, "Could not resend invitation.")
  }
}

export async function revokeTeamInvitation(
  invitationId: number
): Promise<void> {
  try {
    await axiosInstance.delete(`/team-permissions/invitations/${invitationId}`)
  } catch (error) {
    rethrow(error, "Could not revoke invitation.")
  }
}

export async function getTeamAccessActivity(params: {
  page: number
  pageSize: number
}): Promise<AccessActivityList> {
  const { data } = await axiosInstance.get<AccessActivityList>(
    "/team-permissions/access-activity",
    { params }
  )
  return {
    items: data.items ?? [],
    totalCount: data.totalCount ?? 0,
    page: data.page ?? params.page,
    pageSize: data.pageSize ?? params.pageSize,
  }
}
