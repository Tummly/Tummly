import { isAxiosError } from "axios"

import axiosInstance from "@/api/axiosInstance"
import type {
  PrivacyConsentActivityApiItem,
  PrivacyConsentPageApiData,
} from "@/lib/operatorPrivacyConsent/mapPrivacyConsentApiResponse"
import type {
  PermissionRecordsListQueryParams,
  PermissionRecordsListResponse,
} from "@/lib/operatorPrivacyConsent/permissionRecordsListQueryParams"

export type SavePrivacyConsentInput = {
  smsConsentWording?: string
  emailConsentWording?: string
}

export type PatchPrivacyConsentTogglesInput = {
  emailMarketingPermissionEnabled?: boolean
  smsMarketingPermissionEnabled?: boolean
  feedbackFollowUpPermissionEnabled?: boolean
}

function readApiError(error: unknown, fallback: string): string {
  if (isAxiosError(error)) {
    const payload = error.response?.data as { message?: unknown } | undefined
    const message = payload?.message
    if (typeof message === "string" && message.trim() !== "") {
      return message
    }
  }
  return fallback
}

function rethrow(error: unknown, fallback: string): never {
  throw new Error(readApiError(error, fallback))
}

/** Privacy consent page GET — setup rows, toggles, wording, access flags. */
export async function getPrivacyConsent(): Promise<PrivacyConsentPageApiData> {
  try {
    const { data } = await axiosInstance.get<PrivacyConsentPageApiData>(
      "/privacy-consent"
    )
    return {
      ...data,
      privacySetupRows: data.privacySetupRows ?? [],
      smsConsentWording: data.smsConsentWording ?? "",
      emailConsentWording: data.emailConsentWording ?? "",
      actorCanManage: data.actorCanManage === true,
      canViewGuests: data.canViewGuests === true,
    }
  } catch (error) {
    rethrow(error, "Could not load Privacy & consent.")
  }
}

/** Minimal Privacy consent ready + wording save (Locations Setup / ticket 07). */
export async function savePrivacyConsent(
  input: SavePrivacyConsentInput = {}
): Promise<{ privacyReady: boolean }> {
  try {
    const response = await axiosInstance.put<{
      success?: boolean
      privacyReady?: boolean
    }>("/privacy-consent", {
      smsConsentWording: input.smsConsentWording ?? "",
      emailConsentWording: input.emailConsentWording ?? "",
    })
    return {
      privacyReady: response.data.privacyReady === true,
    }
  } catch (error) {
    rethrow(error, "Could not save Privacy consent.")
  }
}

export async function patchPrivacyConsentToggles(
  input: PatchPrivacyConsentTogglesInput
): Promise<void> {
  try {
    await axiosInstance.patch("/privacy-consent", input)
  } catch (error) {
    rethrow(error, "Could not save guest permission.")
  }
}

export async function getPermissionRecords(
  params: PermissionRecordsListQueryParams
): Promise<PermissionRecordsListResponse> {
  try {
    const { data } = await axiosInstance.get<PermissionRecordsListResponse>(
      "/privacy-consent/permission-records",
      { params }
    )
    return {
      success: data.success === true,
      rows: data.rows ?? [],
      totalCount: data.totalCount ?? 0,
      page: data.page ?? params.page ?? 1,
      pageSize: data.pageSize ?? params.pageSize ?? 25,
    }
  } catch (error) {
    rethrow(error, "Could not load permission records.")
  }
}

export async function getPrivacyConsentActivity(): Promise<{
  items: PrivacyConsentActivityApiItem[]
}> {
  try {
    const { data } = await axiosInstance.get<{
      success?: boolean
      items?: PrivacyConsentActivityApiItem[]
    }>("/privacy-consent/activity")
    return {
      items: data.items ?? [],
    }
  } catch (error) {
    rethrow(error, "Could not load privacy activity.")
  }
}
