import axiosInstance from "@/api/axiosInstance"
import type { TeamPermissionsPageData } from "@/lib/operatorTeamPermissions/createOperatorTeamPermissionsPageModule"

export async function getTeamPermissionsPage(): Promise<TeamPermissionsPageData> {
  const { data } = await axiosInstance.get<TeamPermissionsPageData>(
    "/team-permissions"
  )
  return data
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
