import axiosInstance from "./axiosInstance"
import { getUserFacingApiErrorMessage } from "@/lib/apiErrorMessage"

export type MyAccountLocationAccess = {
  locationName: string
  accessLabel: string
}

export type MyAccountSnapshot = {
  fullName: string
  email: string
  jobTitle: string | null
  phoneNumber: string
  role: string
  organisation: string
  locationAccess: MyAccountLocationAccess[]
  twoFactorEnabled: boolean
}

export type UpdateMyAccountProfileInput = {
  fullName: string
  jobTitle: string | null
  phoneNumber: string | null
}

export type ChangePasswordInput = {
  currentPassword: string
  newPassword: string
  confirmNewPassword: string
}

type AccountApiEnvelope = {
  success: boolean
  data: MyAccountSnapshot
  message?: string
}

function readApiMessage(error: unknown, fallback: string): string {
  return getUserFacingApiErrorMessage(error, fallback)
}

export async function getMyAccount(): Promise<MyAccountSnapshot> {
  const response = await axiosInstance.get<AccountApiEnvelope>("/account")
  return response.data.data
}

export async function updateMyAccountProfile(
  input: UpdateMyAccountProfileInput
): Promise<MyAccountSnapshot> {
  try {
    const response = await axiosInstance.patch<AccountApiEnvelope>(
      "/account/profile",
      input
    )
    return response.data.data
  } catch (error) {
    throw new Error(readApiMessage(error, "Unable to save profile changes."))
  }
}

export async function changeMyAccountPassword(
  input: ChangePasswordInput
): Promise<void> {
  try {
    await axiosInstance.post("/account/change-password", input)
  } catch (error) {
    throw new Error(readApiMessage(error, "Unable to change password."))
  }
}
