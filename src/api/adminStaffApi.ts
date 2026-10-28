import { isAxiosError } from "axios"

import axiosInstance from "./axiosInstance"

export const STAFF_ROLES = ["Admin", "Support"] as const

export type StaffRole = (typeof STAFF_ROLES)[number]

export type AdminStaffMember = {
  id: number
  fullName: string
  email: string
  role: StaffRole
  isActive: boolean
  isLocked: boolean
  failedLoginAttempts: number
  createdAt: string
  usesDefaultPassword: boolean
}

export type CreateStaffInput = {
  fullName: string
  email: string
  role: StaffRole
  initialPassword: string
  reason: string
}

export type ResetStaffPasswordInput = {
  newPassword: string
  reason: string
}

export type ChangeOwnPasswordInput = {
  currentPassword: string
  newPassword: string
}

/** Carries the server code (for example email_in_use) so the UI can map it. */
export class StaffApiError extends Error {
  readonly status: number | null
  readonly code: string | null

  constructor(message: string, status: number | null, code: string | null) {
    super(message)
    this.name = "StaffApiError"
    this.status = status
    this.code = code
  }
}

type ErrorBody = {
  message?: string
  code?: string
}

function toApiError(error: unknown, fallback: string): StaffApiError {
  if (!isAxiosError(error)) {
    return new StaffApiError(fallback, null, null)
  }
  const body = (error.response?.data ?? {}) as ErrorBody
  return new StaffApiError(
    body.message ?? fallback,
    error.response?.status ?? null,
    body.code ?? null
  )
}

export async function fetchStaffMembers(): Promise<AdminStaffMember[]> {
  try {
    const { data } = await axiosInstance.get<{
      success: boolean
      items: AdminStaffMember[]
    }>("/admin/staff")
    return data.items
  } catch (error) {
    throw toApiError(error, "Could not load staff accounts.")
  }
}

export async function createStaffMember(
  input: CreateStaffInput
): Promise<AdminStaffMember> {
  try {
    const { data } = await axiosInstance.post<{
      success: boolean
      item: AdminStaffMember
    }>("/admin/staff", input)
    return data.item
  } catch (error) {
    throw toApiError(error, "Could not add the staff account.")
  }
}

async function postStaffAction(
  id: number,
  action: "disable" | "enable" | "unlock",
  reason: string,
  fallback: string
): Promise<AdminStaffMember> {
  try {
    const { data } = await axiosInstance.post<{
      success: boolean
      item: AdminStaffMember
    }>(`/admin/staff/${id}/${action}`, { reason })
    return data.item
  } catch (error) {
    throw toApiError(error, fallback)
  }
}

export function disableStaffMember(id: number, reason: string) {
  return postStaffAction(id, "disable", reason, "Could not disable the account.")
}

export function enableStaffMember(id: number, reason: string) {
  return postStaffAction(id, "enable", reason, "Could not enable the account.")
}

export function unlockStaffMember(id: number, reason: string) {
  return postStaffAction(id, "unlock", reason, "Could not unlock the account.")
}

export async function resetStaffPassword(
  id: number,
  input: ResetStaffPasswordInput
): Promise<AdminStaffMember> {
  try {
    const { data } = await axiosInstance.post<{
      success: boolean
      item: AdminStaffMember
    }>(`/admin/staff/${id}/reset-password`, input)
    return data.item
  } catch (error) {
    throw toApiError(error, "Could not reset the password.")
  }
}

/** Self-service for any signed-in Admin or Support account. */
export async function changeOwnStaffPassword(
  input: ChangeOwnPasswordInput
): Promise<void> {
  try {
    await axiosInstance.post<{ success: boolean }>("/staff/me/password", input)
  } catch (error) {
    throw toApiError(error, "Could not change the password.")
  }
}
