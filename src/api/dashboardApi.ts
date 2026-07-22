import axiosInstance from "./axiosInstance"
import { triggerBrowserDownload as defaultTriggerBrowserDownload } from "@/lib/operatorHome/homeActions"
import type {
  GuestsExportQueryParams,
  GuestsListQueryParams,
} from "@/lib/operatorGuests/guestsListQueryParams"
import {
  mapCreatedGuestTagApiToGuestTag,
  mapGuestTagApiRowToGuestTag,
  type GuestTag,
} from "@/lib/operatorGuests/guestTag"
import type {
  LocationsResponse,
  FeedbackResponse,
  FeedbackDetailsResponse,
  HomeLatestActivityResponse,
  HomePerformanceResponse,
  GuestsResponse,
  GuestProfileResponse,
  CorrectFeedbackClassificationRequest,
  CorrectFeedbackClassificationResponse,
  ChecklistAcksResponse,
  UpdateChecklistAcksRequest,
  FeedbackSentiment,
} from "../types/dashboard"

export const getLocations = async (): Promise<LocationsResponse> => {
  const response = await axiosInstance.get<LocationsResponse>(
    "/restaurant/locations"
  )
  return response.data
}

export const getFeedback = async (
  locationId: number
): Promise<FeedbackResponse> => {
  const response = await axiosInstance.get<FeedbackResponse>("/feedback", {
    params: { locationId },
  })
  return response.data
}

export const getHomeLatestActivity = async (
  locationId: number
): Promise<HomeLatestActivityResponse> => {
  const response = await axiosInstance.get<HomeLatestActivityResponse>(
    "/home/latest-activity",
    { params: { locationId } }
  )
  return response.data
}

function serializeRepeatedParams(
  params: Record<string, unknown>
): string {
  const search = new URLSearchParams()
  for (const [key, value] of Object.entries(params)) {
    if (value == null || value === "") {
      continue
    }
    if (Array.isArray(value)) {
      for (const item of value) {
        if (item == null || item === "") {
          continue
        }
        search.append(key, String(item))
      }
      continue
    }
    search.append(key, String(value))
  }
  return search.toString()
}

export const getGuests = async (
  params: GuestsListQueryParams
): Promise<GuestsResponse> => {
  const response = await axiosInstance.get<GuestsResponse>("/guests", {
    params,
    paramsSerializer: serializeRepeatedParams,
  })
  return response.data
}

function parseContentDispositionFilename(
  header: string | undefined
): string | null {
  if (header == null || header.length === 0) {
    return null
  }
  const utf8Match = /filename\*=UTF-8''([^;]+)/i.exec(header)
  if (utf8Match?.[1]) {
    return decodeURIComponent(utf8Match[1].trim())
  }
  const plainMatch = /filename="?([^";]+)"?/i.exec(header)
  return plainMatch?.[1]?.trim() ?? null
}

export const exportGuestsCsv = async (
  params: GuestsExportQueryParams
): Promise<{ blob: Blob; filename: string }> => {
  const response = await axiosInstance.get<Blob>("/guests/export", {
    params,
    paramsSerializer: serializeRepeatedParams,
    responseType: "blob",
  })
  const filename =
    parseContentDispositionFilename(
      response.headers["content-disposition"] as string | undefined
    ) ?? "tummly-guests.csv"
  return { blob: response.data, filename }
}

export type GuestTagsListResponse = {
  success: boolean
  tags: Array<{
    id: number
    name: string
    guestCount: number
    aiSourced: boolean
  }>
}

export const listGuestTags = async (params: {
  locationId: number
  locationScope?: "all"
  locationIds?: number[]
}): Promise<GuestTag[]> => {
  const response = await axiosInstance.get<GuestTagsListResponse>(
    "/guests/tags",
    {
      params,
      paramsSerializer: serializeRepeatedParams,
    }
  )
  return response.data.tags.map(mapGuestTagApiRowToGuestTag)
}

export const createGuestTag = async (params: {
  locationId: number
  name: string
}): Promise<GuestTag> => {
  const response = await axiosInstance.post<{
    success: boolean
    tag: { id: number; name: string; aiSourced: boolean }
  }>("/guests/tags", { name: params.name }, { params: { locationId: params.locationId } })
  return mapCreatedGuestTagApiToGuestTag(response.data.tag)
}

export const applyGuestTags = async (params: {
  locationId: number
  guestIds: number[]
  tagIds: number[]
}): Promise<void> => {
  await axiosInstance.post(
    "/guests/tags/apply",
    { guestIds: params.guestIds, tagIds: params.tagIds },
    { params: { locationId: params.locationId } }
  )
}

export const getGuestTagMemberships = async (params: {
  locationId: number
  guestIds: number[]
}): Promise<Map<string, string[]>> => {
  const response = await axiosInstance.get<{
    success: boolean
    memberships: Array<{ guestId: number; tagIds: number[] }>
  }>("/guests/tags/memberships", {
    params: {
      locationId: params.locationId,
      guestIds: params.guestIds,
    },
    paramsSerializer: serializeRepeatedParams,
  })

  const map = new Map<string, string[]>()
  for (const row of response.data.memberships) {
    map.set(
      String(row.guestId),
      row.tagIds.map((id) => String(id))
    )
  }
  return map
}

export { defaultTriggerBrowserDownload as triggerBrowserDownload }

export const getGuestProfile = async (params: {
  guestId: number
  locationId: number
}): Promise<GuestProfileResponse> => {
  const response = await axiosInstance.get<GuestProfileResponse>(
    `/guests/${params.guestId}`,
    { params: { locationId: params.locationId } }
  )
  return response.data
}

export const getHomePerformance = async (
  locationId: number,
  from: string,
  to: string
): Promise<HomePerformanceResponse> => {
  const response = await axiosInstance.get<HomePerformanceResponse>(
    "/home/performance",
    { params: { locationId, from, to } }
  )
  return response.data
}

export const getFeedbackDetails = async (
  feedbackId: number
): Promise<FeedbackDetailsResponse> => {
  const response = await axiosInstance.get<FeedbackDetailsResponse>(
    `/feedback/${feedbackId}`
  )
  return response.data
}

export const correctFeedbackClassification = async (
  feedbackId: number,
  sentiment: FeedbackSentiment
): Promise<CorrectFeedbackClassificationResponse> => {
  const body: CorrectFeedbackClassificationRequest = { sentiment }
  const response =
    await axiosInstance.put<CorrectFeedbackClassificationResponse>(
      `/feedback/${feedbackId}/classification`,
      body
    )
  return response.data
}

export const getChecklistAcks = async (
  locationId: number
): Promise<ChecklistAcksResponse> => {
  const response = await axiosInstance.get<ChecklistAcksResponse>(
    "/operator-home/checklist-acks",
    { params: { locationId } }
  )
  return response.data
}

export const setChecklistAcks = async (
  locationId: number,
  body: UpdateChecklistAcksRequest
): Promise<ChecklistAcksResponse> => {
  const response = await axiosInstance.post<ChecklistAcksResponse>(
    "/operator-home/checklist-acks",
    body,
    { params: { locationId } }
  )
  return response.data
}

export const downloadQrCode = async (locationId: number): Promise<Blob> => {
  const response = await axiosInstance.get("/qr/download", {
    params: { locationId },
    responseType: "blob",
  })
  return response.data
}
