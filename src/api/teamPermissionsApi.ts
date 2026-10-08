import { isAxiosError } from "axios"

import { getUserFacingApiErrorMessage } from "@/lib/apiErrorMessage"
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
  }
  return getUserFacingApiErrorMessage(error, fallback)
}

function rethrow(error: unknown, fallback: string): never {
  throw new Error(readApiError(error, fallback))
}

export async function getTeamPermissionsPage(): Promise<TeamPermissionsPageData> {
  try {
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
  } catch (error) {
    rethrow(error, "Could not load team permissions.")
  }
}

export async function updateTeamMemberRole(
  membershipId: number,
  permissionRole: string
): Promise<void> {
  try {
    await axiosInstance.patch(
      `/team-permissions/members/${membershipId}/role`,
      {
        permissionRole,
      }
    )
  } catch (error) {
    rethrow(error, "Could not update member role.")
  }
}

export async function updateTeamMemberLocationScope(
  membershipId: number,
  payload: { locationScope: "all" | "named"; namedLocationIds: number[] }
): Promise<void> {
  try {
    await axiosInstance.patch(
      `/team-permissions/members/${membershipId}/location-scope`,
      payload
    )
  } catch (error) {
    rethrow(error, "Could not update member location scope.")
  }
}

export async function deactivateTeamMember(
  membershipId: number
): Promise<void> {
  try {
    await axiosInstance.post(
      `/team-permissions/members/${membershipId}/deactivate`
    )
  } catch (error) {
    rethrow(error, "Could not deactivate member.")
  }
}

export async function reactivateTeamMember(
  membershipId: number
): Promise<void> {
  try {
    await axiosInstance.post(
      `/team-permissions/members/${membershipId}/reactivate`
    )
  } catch (error) {
    rethrow(error, "Could not reactivate member.")
  }
}

export async function removeTeamMember(membershipId: number): Promise<void> {
  try {
    await axiosInstance.delete(`/team-permissions/members/${membershipId}`)
  } catch (error) {
    rethrow(error, "Could not remove member.")
  }
}

export async function saveTeamPermissionsMatrix(
  adminCells: Array<{ areaId: string; level: string }>
): Promise<void> {
  try {
    await axiosInstance.put("/team-permissions/matrix", { adminCells })
  } catch (error) {
    rethrow(error, "Could not save permission matrix.")
  }
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
  try {
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
  } catch (error) {
    rethrow(error, "Could not load access activity.")
  }
}
