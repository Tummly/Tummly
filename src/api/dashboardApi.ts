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
import type { GuestActivityListQueryParams } from "@/lib/operatorGuestProfile/guestActivityListQueryParams"
import type { GuestFeedbacksListQueryParams } from "@/lib/operatorGuestProfile/guestFeedbacksListQueryParams"
import type {
  LocationsResponse,
  FeedbackResponse,
  FeedbackDetailsResponse,
  HomeLatestActivityResponse,
  HomePerformanceResponse,
  CapturePerformanceResponse,
  CaptureOverviewResponse,
  CaptureLocationsQueryParams,
  CaptureLocationsResponse,
  CaptureLocationCaptureMutationResponse,
  CapturePlacementsResponse,
  CapturePlacementStatusMutationResponse,
  CapturePlacementRotateResponse,
  CaptureArchivedPlacementsResponse,
  CapturePlacementArchiveResponse,
  CapturePlacementRestoreResponse,
  CreateDigitalGuestLinkRequest,
  CreateDigitalGuestLinkResponse,
  GuestsResponse,
  GuestProfileResponse,
  GuestFeedbacksListResponse,
  GuestActivityListResponse,
  GuestNotesListResponse,
  GuestProfileRecentNoteItem,
  CreateGuestNoteResponse,
  CreateFeedbackInternalNoteResponse,
  FeedbackInternalNoteItem,
  PatchGuestIdentityRequest,
  PatchGuestIdentityResponse,
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

/** Sync-replace memberships for guests to exactly `tagIds` (adds + removes). */
export const syncGuestTags = async (params: {
  locationId: number
  guestIds: number[]
  tagIds: number[]
}): Promise<void> => {
  await axiosInstance.post(
    "/guests/tags/sync",
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

export const getGuestFeedbacks = async (
  params: GuestFeedbacksListQueryParams
): Promise<GuestFeedbacksListResponse> => {
  const { guestId, ...query } = params
  const response = await axiosInstance.get<GuestFeedbacksListResponse>(
    `/guests/${guestId}/feedbacks`,
    {
      params: query,
      paramsSerializer: serializeRepeatedParams,
    }
  )
  return response.data
}

export const getGuestActivity = async (
  params: GuestActivityListQueryParams
): Promise<GuestActivityListResponse> => {
  const { guestId, ...query } = params
  const response = await axiosInstance.get<GuestActivityListResponse>(
    `/guests/${guestId}/activity`,
    {
      params: query,
      paramsSerializer: serializeRepeatedParams,
    }
  )
  return response.data
}

export const listGuestNotes = async (params: {
  guestId: number
  locationId: number
  limit?: number
}): Promise<GuestNotesListResponse> => {
  const response = await axiosInstance.get<GuestNotesListResponse>(
    `/guests/${params.guestId}/notes`,
    {
      params: {
        locationId: params.locationId,
        ...(params.limit != null ? { limit: params.limit } : {}),
      },
    }
  )
  return response.data
}

export const createGuestNote = async (params: {
  guestId: number
  locationId: number
  body: string
}): Promise<GuestProfileRecentNoteItem> => {
  const response = await axiosInstance.post<CreateGuestNoteResponse>(
    `/guests/${params.guestId}/notes`,
    { body: params.body },
    { params: { locationId: params.locationId } }
  )
  return response.data.note
}

export const updateGuestNote = async (params: {
  guestId: number
  locationId: number
  noteId: number
  body: string
}): Promise<GuestProfileRecentNoteItem> => {
  const response = await axiosInstance.put<CreateGuestNoteResponse>(
    `/guests/${params.guestId}/notes/${params.noteId}`,
    { body: params.body },
    { params: { locationId: params.locationId } }
  )
  return response.data.note
}

export const softDeleteGuestNote = async (params: {
  guestId: number
  locationId: number
  noteId: number
}): Promise<{ deletedAt: string; deletedByDisplayName: string }> => {
  const response = await axiosInstance.delete<{
    success: boolean
    deletedAt: string
    deletedByDisplayName: string
  }>(`/guests/${params.guestId}/notes/${params.noteId}`, {
    params: { locationId: params.locationId },
  })
  return {
    deletedAt: response.data.deletedAt,
    deletedByDisplayName: response.data.deletedByDisplayName,
  }
}

export const createFeedbackInternalNote = async (params: {
  feedbackId: number
  body: string
}): Promise<FeedbackInternalNoteItem> => {
  const response =
    await axiosInstance.post<CreateFeedbackInternalNoteResponse>(
      `/feedback/${params.feedbackId}/notes`,
      { body: params.body }
    )
  return response.data.note
}

export const updateFeedbackInternalNote = async (params: {
  feedbackId: number
  noteId: number
  body: string
}): Promise<FeedbackInternalNoteItem> => {
  const response =
    await axiosInstance.put<CreateFeedbackInternalNoteResponse>(
      `/feedback/${params.feedbackId}/notes/${params.noteId}`,
      { body: params.body }
    )
  return response.data.note
}

export const softDeleteFeedbackInternalNote = async (params: {
  feedbackId: number
  noteId: number
}): Promise<{ deletedAt: string; deletedByDisplayName: string }> => {
  const response = await axiosInstance.delete<{
    success: boolean
    deletedAt: string
    deletedByDisplayName: string
  }>(`/feedback/${params.feedbackId}/notes/${params.noteId}`)
  return {
    deletedAt: response.data.deletedAt,
    deletedByDisplayName: response.data.deletedByDisplayName,
  }
}

export const patchGuestIdentity = async (params: {
  guestId: number
  locationId: number
  body: PatchGuestIdentityRequest
}): Promise<PatchGuestIdentityResponse> => {
  const response = await axiosInstance.patch<PatchGuestIdentityResponse>(
    `/guests/${params.guestId}`,
    params.body,
    { params: { locationId: params.locationId } }
  )
  return response.data
}

export const deleteLocationGuest = async (params: {
  guestId: number
  locationId: number
}): Promise<void> => {
  await axiosInstance.delete(`/guests/${params.guestId}`, {
    params: { locationId: params.locationId },
  })
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

export const getCapturePerformance = async (
  locationId: number,
  from: string,
  to: string
): Promise<CapturePerformanceResponse> => {
  const response = await axiosInstance.get<CapturePerformanceResponse>(
    "/capture/performance",
    { params: { locationId, from, to } }
  )
  return response.data
}

export const getCaptureOverview = async (
  from: string,
  to: string
): Promise<CaptureOverviewResponse> => {
  const response = await axiosInstance.get<CaptureOverviewResponse>(
    "/capture/overview",
    { params: { from, to } }
  )
  return response.data
}

export const getCaptureLocations = async (
  params: CaptureLocationsQueryParams
): Promise<CaptureLocationsResponse> => {
  const response = await axiosInstance.get<CaptureLocationsResponse>(
    "/capture/locations",
    {
      params: {
        from: params.from,
        to: params.to,
        q: params.q || undefined,
        status: params.status,
        locationIds: params.locationIds,
        sort: params.sort,
        page: params.page,
        pageSize: params.pageSize ?? 20,
      },
      paramsSerializer: serializeRepeatedParams,
    }
  )
  return response.data
}

export const pauseCaptureLocation = async (
  locationId: number
): Promise<CaptureLocationCaptureMutationResponse> => {
  const response =
    await axiosInstance.post<CaptureLocationCaptureMutationResponse>(
      `/capture/locations/${locationId}/pause`
    )
  return response.data
}

export const activateCaptureLocation = async (
  locationId: number
): Promise<CaptureLocationCaptureMutationResponse> => {
  const response =
    await axiosInstance.post<CaptureLocationCaptureMutationResponse>(
      `/capture/locations/${locationId}/activate`
    )
  return response.data
}

export const getCapturePlacements = async (
  locationId: number,
  from: string,
  to: string
): Promise<CapturePlacementsResponse> => {
  const response = await axiosInstance.get<CapturePlacementsResponse>(
    "/capture/placements",
    { params: { locationId, from, to } }
  )
  return response.data
}

export const createDigitalGuestLink = async (
  locationId: number,
  body: CreateDigitalGuestLinkRequest
): Promise<CreateDigitalGuestLinkResponse> => {
  const response = await axiosInstance.post<CreateDigitalGuestLinkResponse>(
    "/capture/placements/digital-guest-links",
    body,
    { params: { locationId } }
  )
  return response.data
}

export const pauseCapturePlacement = async (
  locationId: number,
  qrCodeId: number
): Promise<CapturePlacementStatusMutationResponse> => {
  const response =
    await axiosInstance.post<CapturePlacementStatusMutationResponse>(
      `/capture/placements/${qrCodeId}/pause`,
      null,
      { params: { locationId } }
    )
  return response.data
}

export const resumeCapturePlacement = async (
  locationId: number,
  qrCodeId: number
): Promise<CapturePlacementStatusMutationResponse> => {
  const response =
    await axiosInstance.post<CapturePlacementStatusMutationResponse>(
      `/capture/placements/${qrCodeId}/resume`,
      null,
      { params: { locationId } }
    )
  return response.data
}

export const rotateCapturePlacement = async (
  locationId: number,
  qrCodeId: number
): Promise<CapturePlacementRotateResponse> => {
  const response = await axiosInstance.post<CapturePlacementRotateResponse>(
    `/capture/placements/${qrCodeId}/rotate`,
    null,
    { params: { locationId } }
  )
  return response.data
}

export const getArchivedCapturePlacements =
  async (): Promise<CaptureArchivedPlacementsResponse> => {
    const response = await axiosInstance.get<CaptureArchivedPlacementsResponse>(
      "/capture/placements/archived"
    )
    return response.data
  }

export const archiveCapturePlacement = async (
  locationId: number,
  qrCodeId: number
): Promise<CapturePlacementArchiveResponse> => {
  const response =
    await axiosInstance.post<CapturePlacementArchiveResponse>(
      `/capture/placements/${qrCodeId}/archive`,
      null,
      { params: { locationId } }
    )
  return response.data
}

export const restoreCapturePlacement = async (
  locationId: number,
  qrCodeId: number
): Promise<CapturePlacementRestoreResponse> => {
  const response =
    await axiosInstance.post<CapturePlacementRestoreResponse>(
      `/capture/placements/${qrCodeId}/restore`,
      null,
      { params: { locationId } }
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

