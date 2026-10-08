import axiosInstance from "@/api/axiosInstance"
import { getUserFacingApiErrorMessage } from "@/lib/apiErrorMessage"

const skipAuth = { skipAuthRedirect: true } as const

export type TeamInvitationPreview = {
  email: string
  fullName: string
  workspaceName: string
  roleName: string
  locationScope: string
  existingUser: boolean
  session: "logged-out" | "invited-email" | "wrong-email"
  ownerActivation: "ok" | "pending" | "expired"
}

export type TeamInvitationSession = {
  token: string
  refreshToken: string | null
  accountType: string
  workspaceCount: number
  selectedLocationId: number | null
  activationRequired: boolean
  ownerActivation: "ok" | "pending" | "expired"
}

function rethrow(error: unknown, fallback: string): never {
  throw new Error(getUserFacingApiErrorMessage(error, fallback))
}

export async function previewTeamInvitation(
  invite: string
): Promise<TeamInvitationPreview> {
  try {
    const { data } = await axiosInstance.get<TeamInvitationPreview>(
      "/team-invitations/preview",
      { params: { invite }, ...skipAuth }
    )
    return data
  } catch (error) {
    rethrow(error, "This invitation is not valid.")
  }
}

export async function submitTeamInvitationCredentials(payload: {
  invite: string
  fullName: string
  password: string
}): Promise<void> {
  try {
    await axiosInstance.post(
      "/team-invitations/credentials",
      payload,
      skipAuth
    )
  } catch (error) {
    rethrow(error, "Could not save your details.")
  }
}

export async function submitTeamInvitationSignIn(payload: {
  invite: string
  password: string
}): Promise<void> {
  try {
    await axiosInstance.post("/team-invitations/sign-in", payload, skipAuth)
  } catch (error) {
    rethrow(error, "Could not sign in.")
  }
}

export async function verifyTeamInvitationOtp(payload: {
  invite: string
  email: string
  otpCode: string
}): Promise<TeamInvitationSession> {
  try {
    const { data } = await axiosInstance.post<TeamInvitationSession>(
      "/team-invitations/verify-otp",
      payload,
      skipAuth
    )
    return data
  } catch (error) {
    rethrow(error, "Invalid OTP.")
  }
}

export async function acceptTeamInvitationInPlace(
  invite: string
): Promise<TeamInvitationSession> {
  try {
    const { data } = await axiosInstance.post<TeamInvitationSession>(
      "/team-invitations/accept",
      {},
      { params: { invite }, ...skipAuth }
    )
    return data
  } catch (error) {
    rethrow(error, "Could not accept invitation.")
  }
}
